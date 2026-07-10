using System.Diagnostics;
using SPTarkov.Common.Models.Logging;

namespace WTTServerCommonLib.Helpers;

public static class LogHelper
{
    [Conditional("DEBUG")]
    public static void Debug<T>(ISptLogger<T> logger, string message)
    {
        logger.Info(message);
    }
}
