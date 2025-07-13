using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TidyData.Azure.Tests.Helpers;

public static class EnvironmentHelpers
{
    public static bool IsRunningOnServer()
    {
        bool inGithubActions = XmlConvert.ToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")?.ToLower() ?? "0");
        if (inGithubActions)
            return true;

        bool inCI = XmlConvert.ToBoolean(Environment.GetEnvironmentVariable("CI")?.ToLower() ?? "0");
        if (inCI)
            return true;

        return false;
    }
}
