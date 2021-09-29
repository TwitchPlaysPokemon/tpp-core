namespace TPP.Core.Overlay.Events;

public struct MatchCreatedEvent : IOverlayEvent
{
    public string OverlayEventType => "match_created";
}
public struct MatchBettingEvent : IOverlayEvent
{
    public string OverlayEventType => "match_betting";
}
public struct MatchWarningEvent : IOverlayEvent
{
    public string OverlayEventType => "match_warning";
}
public struct ResultsFinishedEvent : IOverlayEvent
{
    public string OverlayEventType => "results_finished";
}
