Here lives the code that handles automated chat moderation.
The automated moderation, also known as modbot, operates by putting each message through a list of [rules](Rules.cs).
Each rule returns an approproate moderative action (see [RuleResult](IModerationRule.cs)).
The rules are orchestrated in the [Moderator](Moderator.cs) class.
