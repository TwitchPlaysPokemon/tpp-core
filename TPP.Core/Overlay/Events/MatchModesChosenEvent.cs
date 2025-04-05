namespace TPP.Core.Overlay.Events;

public struct MatchModesChosenEvent : IOverlayEvent
{
    public string OverlayEventType => "match_modes_chosen";

    // TODO, see modes.js in old core:
    //     var metagame_id = extra.metagame_id;
    //     var metagame_name = extra.metagame_name;
    //     var metagame_icon = extra.metagame_icon;
    //     var metagame_description = extra.metagame_description;
    //     var metagame_base_icons = extra.metagame_base_icons;
    //     var metagame_base_short_descriptions = extra.metagame_base_short_descriptions;
    //     var gimmick_id = extra.gimmick_id;
    //     var gimmick_name = extra.gimmick_name;
    //     var gimmick_icon = extra.gimmick_icon;
    //     var gimmick_description = extra.gimmick_description;
    //     var gimmick_base_icons = extra.gimmick_base_icons;
    //     var gimmick_base_short_descriptions = extra.gimmick_base_short_descriptions;
    //     var metagame_roulette_dummies = extra.metagame_roulette_dummies;
    //     var gimmick_roulette_dummies = extra.gimmick_roulette_dummies;
    //     // Bet bonus changes depending on the metagame and gimmick present.
    //     var bet_bonus = extra.bet_bonus
    //     var display_bet_bonus = extra.display_bet_bonus

}
