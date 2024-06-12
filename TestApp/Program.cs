using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarPx;
namespace TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var apiClient = new Client("http://localhost:3000");

            try
            {
                var authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ...";


                var authData = "valid-api-key";
                var authResult = await apiClient.Authenticate("/authenticate", authData);
                Console.WriteLine(authResult);

                var postResult = await apiClient.PlateSolve("/platesolve", authToken);
                Console.WriteLine(postResult);

                var result = await apiClient.PlateSolveResult("/platesolve/platesolve123", authToken);
                Console.WriteLine(result);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
