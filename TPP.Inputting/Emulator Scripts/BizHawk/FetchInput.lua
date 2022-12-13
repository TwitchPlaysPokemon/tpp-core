local getInputEndpoint = "http://127.0.0.1:5010/gbmode_input_request_bizhawk"
local doneInputEndpoint = "http://127.0.0.1:5010/gbmode_input_complete"
local saveStateFolder = "" --must end in / (or \) (or blank is fine)

local inputEnabled = true
local saveStateEnabled = true

local inputMaxHeldFrames = 16 -- override's core's value. Change to fine-tune input speed per-game. Sync with core's values to keep input feed regular


-- http will be provided by TPP-Bizhawk or Luasocket library
if http == nil then
	http = require("socket.http")
	http.TIMEOUT = 0.01
end
JSON = (loadfile "JSON.lua")()

local currentInput = {}
local currentSeries = {}
function fetchInput()
	if inputEnabled ~= true then
		return
	end

	if currentInput['active'] ~= true then --We don't have an input.
		if table.getn(currentSeries) > 0 then --get next input from series
			currentInput = table.remove(currentSeries, 1)
            if currentInput["Held_Frames"] ~= nil then
				currentInput["Held_Frames"] = math.min(currentInput["Held_Frames"], inputMaxHeldFrames)
			end
			currentInput['active'] = true
		else --Check the core
			local response, status = http.request(getInputEndpoint)
			--print(response)
			if status == 200 then
				local newInput = JSON:decode(response)
                if newInput["Input_Id"] and currentInput["Input_Id"] and newInput["Input_Id"] == currentInput["Input_Id"] then
                    -- Didn't get new input. Call Done endpoint again, try again next frame
                    return http.request(doneInputEndpoint)
                end
                currentInput = newInput
                if currentInput["Held_Frames"] ~= nil then
                    currentInput["Held_Frames"] = math.min(currentInput["Held_Frames"], inputMaxHeldFrames)
                end
                currentInput['active'] = true
                if currentInput["Series"] ~= nil then
                    currentSeries = currentInput["Series"]
                    currentInput = {}
                    return fetchInput()
				else
					currentInput["Held_Frames"] = math.ceil(currentInput["Held_Frames"])
					currentInput["Sleep_Frames"] = math.ceil(currentInput["Sleep_Frames"])
					if currentInput["Hold"] == true then
						currentInput["Hold"] = false
						local shiftFrames = 24 - currentInput["Held_Frames"]
						currentInput["Held_Frames"] = currentInput["Held_Frames"] + shiftFrames
						currentInput["Sleep_Frames"] = currentInput["Sleep_Frames"] - shiftFrames
						if currentInput["Sleep_Frames"] < 0 then
							currentInput["Held_Frames"] = currentInput["Held_Frames"] + currentInput["Sleep_Frames"]
						end
					end
				end
            end
		end
	end

	local input = currentInput
	if currentInput["Held_Frames"] ~= nil and currentInput["Held_Frames"] > 0 then --time of press
		currentInput["Held_Frames"] = currentInput["Held_Frames"] - 1
	elseif currentInput["Sleep_Frames"] ~= nil and currentInput["Sleep_Frames"] > 0 then --time between presses
		currentInput["Sleep_Frames"] = currentInput["Sleep_Frames"] - 1
        if currentInput["Hold"] ~= true then
		    input = {}
        end
	elseif currentInput['active'] then
        if currentInput["Hold"] ~= true and currentInput["Sleep_Frames"] == 0 then  --if sleep frames are negative this was a held input
    		input = {}
            currentInput = {}
        else
            currentInput['active'] = false
        end
		if table.getn(currentSeries) < 1 then
			http.request(doneInputEndpoint)
		end
	end

	joypad.set(input)
end


function saveState()
	if emu.framecount() % 18000 == 1 and saveStateEnabled then --every 5 minutes
		local time = os.date("*t")
		local path = string.format("%s%04d-%02d-%02dT%02d-%02d-%02dZ.%s.State", saveStateFolder, time['year'], time['month'], time['day'], time['hour'], time['min'], time['sec'], gameinfo.getromname())
		savestate.save(path)
	end
end

if not JSON then --We're running by ourselves, run our own loop
	JSON = (loadfile "JSON.lua")()
	while true do
		emu.frameadvance()
		fetchInput()
		saveState()
	end
end
