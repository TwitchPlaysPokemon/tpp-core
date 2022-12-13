local getInputEndpoint = "http://127.0.0.1:5010/gbmode_input_request"
local doneInputEndpoint = "http://127.0.0.1:5010/gbmode_input_complete"
local backupEndpoint = nil -- not currently supported by new core

local maxHeldFrames = 24

-- http provided by Luasocket library
local http = require("socket.http")
http.TIMEOUT = 0.01


local saveEveryFrames = (60*60*5)
local framesSinceLastSave = 0
function saveState(slot, force) 
	framesSinceLastSave = framesSinceLastSave + 1
	if force or (framesSinceLastSave >= saveEveryFrames) then
		framesSinceLastSave = 0
        local st = savestate.create(slot or 1)
        savestate.save(st)
        if backupEndpoint then
            http.request(backupEndpoint)
		end
		local time = os.date("*t")
		local timeStr = string.format("%04d-%02d-%02dT%02d:%02d:%02dZ", time['year'], time['month'], time['day'], time['hour'], time['min'], time['sec'])
		print("Saved to slot " .. (slot or 1) .. " at " .. timeStr)
		return true
	end
	return false
end

function panicRestore()
	savestate.load(2)
end

local currentInput = {}
local currentSeries = {}

function initInput(input)
    if input["Input_Id"] and currentInput["Input_Id"] and input["Input_Id"] == currentInput["Input_Id"] then
        -- Didn't get new input. Call Done endpoint again, try again next frame
        return http.request(doneInputEndpoint)
    end
    currentInput = input
    currentInput['active'] = true
    if currentInput["series"] ~= nil then
        currentSeries = currentInput["series"]
        currentInput = { ["active"] = true }
    else
        currentInput["held_frames"] = math.ceil(currentInput["held_frames"] or 0)
        currentInput["sleep_frames"] = math.ceil(currentInput["sleep_frames"] or 0)
        if currentInput["hold"] == true or (input["touch_screen_x2"] and input["touch_screen_y2"]) then --force hold on drags
            currentInput["hold"] = false
            local shiftFrames = maxHeldFrames - currentInput["held_frames"]
            currentInput["held_frames"] = currentInput["held_frames"] + shiftFrames
            currentInput["sleep_frames"] = currentInput["sleep_frames"] - shiftFrames
            if currentInput["sleep_frames"] < 0 then
                currentInput["held_frames"] = currentInput["held_frames"] + currentInput["sleep_frames"]
            end
		end
		currentInput["total_held_frames"] = currentInput["held_frames"] -- used for easing drags
    end
end

function fetchInput()
	if currentInput['active'] ~= true then --We don't have an input.
		if table.getn(currentSeries) > 0 then --get next input from series
			initInput(table.remove(currentSeries, 1))
		else --Check the core
			local response = http.request(getInputEndpoint)
			if response ~= nil and response ~= "Unable to connect to the remote server" then			
				initInput(JSON:decode(response))
				--print(response)
			end
		end
	end

	if currentInput["admin_command"] == "Panic Restore" then
		panicRestore()
		currentInput = { ["active"] = true }
	end

	local input = currentInput

	if currentInput["held_frames"] ~= nil and currentInput["held_frames"] > 0 then --time of press
		currentInput["held_frames"] = currentInput["held_frames"] - 1
	elseif currentInput["sleep_frames"] ~= nil and currentInput["sleep_frames"] > 0 then --time between presses
		currentInput["sleep_frames"] = currentInput["sleep_frames"] - 1
		if not currentInput["hold"] then
			input = {}
		end
	elseif currentInput['active'] then
		if not currentInput["hold"] and currentInput["sleep_frames"] == 0 then --if sleep frames are negative this was a held input
			input = {}
			currentInput = {}
		else
			currentInput['active'] = false
		end
		if table.getn(currentSeries) < 1 then
			http.request(doneInputEndpoint)
		end
	else
		if currentInput["expired"] == nil then
			currentInput["expired"] = 1
		elseif currentInput["expired"] > 10 then --even if held, shred input after 10 frames of not recieving a new input
			currentInput = {}
		else
			currentInput["expired"] = currentInput["expired"] + 1
		end
	end
	if input["touch_screen_x"] and input["touch_screen_y"] then
		local x = math.floor(ease(currentInput["held_frames"] or 0, input["touch_screen_x2"] or input["touch_screen_x"], input["touch_screen_x"], (currentInput["total_held_frames"] or 16) - 1)) --held_frames already ticked down 1
		local y = math.floor(ease(currentInput["held_frames"] or 0, input["touch_screen_y2"] or input["touch_screen_y"], input["touch_screen_y"], (currentInput["total_held_frames"] or 16) - 1)) --so we take one from the total
        --print({x=x, y=y, touch=true})
		stylus.set({x=x, y=y, touch=true})
	end
	joypad.set(1, input)
end

function ease(elapsedFrames, startValue, endValue, totalFrames)
	return (endValue - startValue) * elapsedFrames / totalFrames + startValue --linear easing
end


if not JSON then --We're running by ourselves, run our own loop
	JSON = (loadfile "JSON.lua")()
	while true do
		emu.frameadvance()
		fetchInput()
		saveState()
	end
end
