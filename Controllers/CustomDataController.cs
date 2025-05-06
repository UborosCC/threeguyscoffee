using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Easyweb.Controllers 
{

    public class CustomDataController : Controller 
    {
        private readonly HttpClient _httpClient; 

        public CustomDataController()
        {
            _httpClient = new HttpClient();
        }

        private async Task<IActionResult> FetchFromSwapi(string endpoint) 
        {
            try 
            {

            var response = await _httpClient.GetAsync($"https://swapi.info/api/{endpoint}");
            //om SWAPI skickar tillbaka någon error, t.ex 404
            if (!response.IsSuccessStatusCode) 
            {
                return StatusCode((int)response.StatusCode, $"Error from SWAPI: {response.ReasonPhrase}");
            }

            var result =  await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
            }
            catch (HttpRequestException httpEx)
            {
                //Nätverk, DNS problem och sånt
                return StatusCode(503, "Unable to reach SWAPI: " + httpEx.Message);
            }
            catch (TaskCanceledException timeoutEx)
            {
                //time out på request
                return StatusCode(504, "SWAPI request time out: " + timeoutEx.Message);
            }
            catch (Exception ex)
            {
                //Om något oväntad händer
                return StatusCode(500, "Unexpected Error: " + ex.Message);
            }
        }

        //Hämtar people från SWAPI
        [HttpGet("/data/people")]
        public Task<IActionResult> GetPeople() => FetchFromSwapi("people");

        //Hämtar planets från SWAPI
        [HttpGet("/data/planets")]
        public Task<IActionResult> GetPlanets() => FetchFromSwapi("planets");

        //Hämtar filmer från SWAPI
        [HttpGet("/data/films")]
        public Task<IActionResult> GetFilms() => FetchFromSwapi("films");

        //Hämtar species från SWAPI
        [HttpGet("/data/species")]
        public Task<IActionResult> GetSpecies() => FetchFromSwapi("species");

        //Hämtar fordon från SWAPI
        [HttpGet("/data/vehicles")]
        public Task<IActionResult> GetVehicles() => FetchFromSwapi("vehicles");

        //Hämtar starships från SWAPI
        [HttpGet("/data/starships")]
        public Task<IActionResult> GetStarships() => FetchFromSwapi("starships");

        //Hämtar people från SWAPI genom sökning med namn
        [HttpGet("/data/people/search")]
        public async Task<IActionResult> SearchEnrichedPeople([FromQuery] string name)
        {
            //Om inget finns på search, skicka error
            if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Please provide a name");

            try 
            {
            var response = await _httpClient.GetAsync("https://swapi.info/api/people");
            if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, $"Error from SWAPI: {response.ReasonPhrase}");

            var result = await response.Content.ReadAsStringAsync();
            var peopleData = JsonDocument.Parse(result);
            var peopleArray = peopleData.RootElement;

            if (peopleArray.ValueKind != JsonValueKind.Array)
            return StatusCode(500, "Unexpected data format from API.");

            var matches = new List<Dictionary<string, object>>();

            foreach (var person in peopleArray.EnumerateArray())
            {
                var personName = person.GetProperty("name").GetString();
                if (!string.IsNullOrEmpty(personName) && personName.ToLower().Contains(name.ToLower()))
                {
                     var enrichedPerson = JsonSerializer.Deserialize<Dictionary<string, object>>(person.GetRawText());

                    // Enrich Homeworld
                    if (person.TryGetProperty("homeworld", out JsonElement homeworldProp))
                    {
                        var homeworldUrl = homeworldProp.GetString();
                        if (!string.IsNullOrEmpty(homeworldUrl))
                        {
                            try
                            {
                                var homeworldResponse = await _httpClient.GetStringAsync(homeworldUrl);
                                var homeworldJson = JsonDocument.Parse(homeworldResponse).RootElement;

                                enrichedPerson["homeworld"] = new
                                {
                                    name = homeworldJson.GetProperty("name").GetString(),
                                    climate = homeworldJson.GetProperty("climate").GetString()
                                };
                            }
                            catch
                            {
                                enrichedPerson["homeworld"] = "Failed to load";
                            }
                        }
                    }

                    // Enrich Films
                    if (person.TryGetProperty("films", out JsonElement filmsProp) && filmsProp.ValueKind == JsonValueKind.Array)
                    {
                        var filmList = new List<object>();
                        foreach (var filmUrlElement in filmsProp.EnumerateArray())
                        {
                            var filmUrl = filmUrlElement.GetString();
                            if (!string.IsNullOrEmpty(filmUrl))
                            {
                                try
                                {
                                    var filmResponse = await _httpClient.GetStringAsync(filmUrl);
                                    var filmJson = JsonDocument.Parse(filmResponse).RootElement;

                                    filmList.Add(new
                                    {
                                        title = filmJson.GetProperty("title").GetString(),
                                        episode = filmJson.GetProperty("episode_id").GetInt32()
                                    });
                                }
                                catch
                                {
                                    filmList.Add(new { title = "Failed to load", episode = -1 });
                                }
                            }
                        }

                        enrichedPerson["films"] = filmList;
                    }

                    //Enrich vehicles
                    if (person.TryGetProperty("vehicles", out JsonElement vehiclesProp) && vehiclesProp.ValueKind == JsonValueKind.Array)
                    {
                        var vehicleList = new List<object>();

                        foreach (var vehicleUrlElement in vehiclesProp.EnumerateArray())
                        {
                            var vehicleUrl = vehicleUrlElement.GetString();
                            if (!string.IsNullOrEmpty(vehicleUrl))
                            {
                                try 
                                {
                                    var vehicleResponse = await _httpClient.GetStringAsync(vehicleUrl);
                                    var vehicleJson = JsonDocument.Parse(vehicleResponse).RootElement;

                                    vehicleList.Add(new {
                                        name = vehicleJson.GetProperty("name").GetString(),
                                        model = vehicleJson.GetProperty("model").GetString()
                                    });
                                }
                                catch
                                {
                                    vehicleList.Add(new { name = "Error", model = "N/A"});
                                }
                            }
                        }

                        enrichedPerson["vehicles"] = vehicleList;
                    }

                    //Enrich starships
                    if (person.TryGetProperty("starships", out JsonElement starshipsProp) && starshipsProp.ValueKind == JsonValueKind.Array)
                    {
                        var starshipList = new List<object>();

                        foreach (var starshpiUrlElement in starshipsProp.EnumerateArray())
                        {
                            var starsihpUrl = starshpiUrlElement.GetString();
                            if (!string.IsNullOrEmpty(starsihpUrl))
                            {
                                try
                                {
                                    var starshipResponse = await _httpClient.GetStringAsync(starsihpUrl);
                                    var starshipJson = JsonDocument.Parse(starshipResponse).RootElement;

                                    starshipList.Add(new {
                                        name = starshipJson.GetProperty("name").GetString(),
                                        model = starshipJson.GetProperty("model").GetString()
                                    });
                                }
                                catch 
                                {
                                    starshipList.Add(new { name = "Error", model = "N/A" });
                                }
                            }
                        }
                        
                        enrichedPerson["starships"] = starshipList;
                    }

                    matches.Add(enrichedPerson);
                }
            }


            if (matches.Count > 0)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(matches, options);
                return Content(json, "application/json");
            }

            return NotFound("Character not found");
            }
            catch (HttpRequestException httpEx)
            {
                return StatusCode(503, "Could not contact SWAPI: " + httpEx.Message);
            }
            catch (JsonException jsonEx)
            {
                return StatusCode(500, "Error Parsing JSON Data: " + jsonEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unexpected Error: " + ex.Message);
            }
        }
    }
}