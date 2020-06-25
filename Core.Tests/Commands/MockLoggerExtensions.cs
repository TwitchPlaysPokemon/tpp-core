using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Core.Tests.Commands
{
    /// <summary>
    /// Extension methods for easier verification of logging calls on moq mocks.
    /// Example: <code>loggerMock.VerifyLog(LogLevel.Warning, "oh no!", Times.Once());</code>
    /// Based on https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80#0b05
    /// Fix for .net core 3.1 from https://github.com/moq/moq4/issues/918#issuecomment-527647423
    /// </summary>
    public static class MockLoggerExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock,
            LogLevel level, string message, string? failMessage = null)
        {
            loggerMock.VerifyLog(level, message, Times.Once(), null, failMessage);
        }

        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock,
            LogLevel level, Regex messageRegex, string? failMessage = null)
        {
            loggerMock.VerifyLog(level, messageRegex, Times.Once(), null, failMessage);
        }

        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock,
            LogLevel level, string message, Times times, Exception? exception = null, string? failMessage = null)
        {
            loggerMock.Verify(l => l.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<object>(o => o.ToString() == message),
                    exception == null
                        ? null
                        : It.Is<Exception>(e => e.GetType() == exception.GetType() && e.Message == exception.Message),
                    (Func<object, Exception, string>)It.IsAny<object>()
                ), times, failMessage
            );
        }

        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock,
            LogLevel level, Regex messageRegex, Times times, Exception? exception = null, string? failMessage = null)
        {
            loggerMock.Verify(l => l.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<object>(o => o != null && messageRegex.Match(o.ToString()!).Success),
                    It.Is<Exception>(e => exception == null
                                          || e.GetType() == exception.GetType() && e.Message == exception.Message),
                    (Func<object, Exception, string>)It.IsAny<object>()
                ), times, failMessage
            );
        }
    }
}
