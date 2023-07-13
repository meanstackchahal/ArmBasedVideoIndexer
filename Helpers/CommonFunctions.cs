using ArmBasedVideoIndexer.Models;
using System.Text.Json;
using System.Web;
using static ArmBasedVideoIndexer.Helpers.GlobalConstants;

namespace ArmBasedVideoIndexer.Helpers
{
    public static class CommonFunctions
    {
        public static void VerifyStatus(HttpResponseMessage response, System.Net.HttpStatusCode excpectedStatusCode)
        {
            if (response.StatusCode != excpectedStatusCode)
            {
                throw new Exception(response.ToString());
            }
        }

       public static string CreateQueryString(IDictionary<string, string> parameters)
        {
            var queryParameters = HttpUtility.ParseQueryString(string.Empty);
            foreach (var parameter in parameters)
            {
                queryParameters[parameter.Key] = parameter.Value;
            }

            return queryParameters.ToString();
        }
       public static string AddExcludedAIs(string excludedAI)
        {
            if (String.IsNullOrEmpty(excludedAI))
            {
                return "";
            }
            var list = excludedAI.Split(',');
            var result = "";
            foreach (var item in list)
            {
                result += "&excludedAI=" + item;
            }
            return result;
        }
    } 
}
