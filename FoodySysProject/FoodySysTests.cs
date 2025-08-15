using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using FoodySysProject.Models;
using System.Text.Json.Nodes;


namespace FoodySysProject
{
    [TestFixture]
    public class FoodySysTests
    {
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";
        private const string LogiUserName = "smo111";
        private const string LogiPassword = "12345678";
        private const string ConstToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJkMGY2YjAwNS01ZWE2LTRiZmEtOGY4ZC02MzMxNDdhMDFhN2QiLCJpYXQiOiIwOC8xNS8yMDI1IDA2OjA1OjU2IiwiVXNlcklkIjoiYjJjODdmOTAtMDhhNy00MDViLTlhYzctMDhkZGQ4ZTVkYWIyIiwiRW1haWwiOiJzbW8xMTFAc21vLmNvbSIsIlVzZXJOYW1lIjoic21vMTExIiwiZXhwIjoxNzU1MjU5NTU2LCJpc3MiOiJGb29keV9BcHBfU29mdFVuaSIsImF1ZCI6IkZvb2R5X1dlYkFQSV9Tb2Z0VW5pIn0.Zeu-Ml30q33JGvHN0qoXqMTEjXuV4Ti3b6BZ7gIudsk";
        private string LastCreatedFoodId = "d0f6b005-5ea6-4bfa-8f8d-633147a01a7d"; 
        private RestClient client;


        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = "";

            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                jwtToken = ConstToken;

            }
            else
            {
                jwtToken = GetJwtToken(LogiUserName, LogiPassword);

            }

            var options = new RestClientOptions(BaseUrl)
            {
               
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);

        }

        private string GetJwtToken(string username, string password)
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responsed = JsonSerializer.Deserialize<JsonElement>(response.Content);
                string token = responsed.GetProperty("accessToken").ToString();
                return token;

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token is null or empty");
                }

               
            }

            else
            {
                throw new InvalidOperationException($"Failed to get JWT token: {response.StatusCode} - {response.Content}");

            }
        }

        [Order(1)]
        [Test]
        public void CreateNewFoodWithRequiredFieldsShouldReturnSuccess()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/Food/Create", Method.Post);
            var food = new FoodDTO
            {
                Name = "TestFood",
                Description = "TestDescription",               
                Url = ""
            };
            request.AddJsonBody(food);
            var response = this.client.Execute(request);
            var responseContent = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 OK");
            Assert.That(responseContent.FoodId, Is.Not.Null.Or.Empty, "Response content contains foodId");

            LastCreatedFoodId = responseContent.FoodId;

        }

        [Order(2)]
        [Test]
        public void EditedTheTitleOfCreatedFoodShouldReturnSuccess()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest($"/api/Food/Edit/{LastCreatedFoodId}", Method.Patch);

            // Fix: Use JsonArray with JsonObject for PATCH body
            var reqBody = new JsonArray
            {
                new JsonObject
                {
                    ["path"] = "/name",
                    ["op"] = "replace",
                    ["value"] = "Edited Title"
                }
            };
            request.AddJsonBody(reqBody);

            var response = this.client.Execute(request);
            var responseContent = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            string message = responseContent.Message;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(message, Is.EqualTo("Successfully edited"), "Response message should indicate success");
        }

        [Order(3)]
        [Test]
        public void GettingAllFoodsShouldReturnSuccess()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/Food/All");
            var response = this.client.Execute(request);
            // Fix: Use List<FoodDTO> for deserialization of array of FoodDTO
            var arrayResponseContent = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(arrayResponseContent, Is.Not.Null.Or.Empty, "Array should be not empty");
        }

        [Order(4)]
        [Test]
        public void DeleteCreatedFoodShouldReturnSuccess()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest($"/api/Food/Delete/{LastCreatedFoodId}", Method.Delete);
            var response = this.client.Execute(request);
            var responseContent = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(responseContent.Message, Is.EqualTo("Deleted successfully!"), "Response message should indicate success");
        }

        [Order(5)]
        [Test]
        public void CreatingFoodWithoutRequiredFieldsShouldFail()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/Food/Create", Method.Post);
            var food = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };
            request.AddJsonBody(food);
            var response = this.client.Execute(request);
            var responseContent = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
          
        }

        [Order(6)]
        [Test]
        public void EditingNonExistingFoodShouldFail()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/Food/Edit/NonExistingFoodId", Method.Patch);
            var reqBody = new JsonArray
            {
                new JsonObject
                {
                    ["path"] = "/name",
                    ["op"] = "replace",
                    ["value"] = "Edited Title"
                }
            };
            request.AddJsonBody(reqBody);
            var response = this.client.Execute(request);
            var responseContent = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 Not Found");
            Assert.That(responseContent.Message, Is.EqualTo("No food revues..."), "Response message should indicate food not found");

        }

        [Order(7)]
        [Test]
        public void DeletingNonExistingFoodShouldFail()
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/Food/Delete/NonExistingFoodId", Method.Delete);
            var response = this.client.Execute(request);
            var responseContent = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request");
            Assert.That(responseContent.Message, Is.EqualTo("Unable to delete this food revue!"), "Response message should indicate food not found");
        }


        [OneTimeTearDown]
        public void TearDown()
        {
           this.client?.Dispose();
            }
    }
}