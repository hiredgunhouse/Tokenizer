using System;
using System.Configuration;

namespace Tokenizer
{
    internal static class Config
    {
        public static bool WarnAboutTokensNotFoundInFile => 
            Convert.ToBoolean(ConfigurationManager.AppSettings["WarnAboutTokensNotFoundInFile"] ?? "true"); 
    }
}