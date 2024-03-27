using Eventmi.Core.Models.Event;
using Eventmi.Infrastructure.Data.Contexts;
using Eventmi.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using RestSharp;

namespace Eventmi.Tests
{
    [TestFixture]
    public class EventControllerTests

    {
        private RestClient _client;
        private const string baseUrl = @"https://localhost:7236";

        [SetUp]
        public void Setup()
        {
            _client = new RestClient(baseUrl);
        }

        [Test]
        public void GetAllEvents_ReturnsSuccessStatusCode()
        {
            //Arrange
            var request = new RestRequest("/Event/All", Method.Get);

            //Act
            var response = _client.Execute(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public void GetRequest_ReturnsAddView() 
        {
            //Arrange
            var request = new RestRequest("/Event/Add", Method.Get);

            //Act
            var response = _client.Execute(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public void Add_PostRequest_AddsEventAndRedirekts()
        {
            //Arrange

            var newModel = new EventFormModel
            {
                Name = "New Event",
                Start = new DateTime(2024, 03, 20, 09, 10, 10),
                End = new DateTime(2024, 03, 21, 09, 10, 10),
                Place = "Plovdiv"
            };

            var request = new RestRequest("/Event/Add", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Name", newModel.Name);
            request.AddParameter("Start", newModel.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", newModel.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", newModel.Place);

            //Act
            var response = _client.Execute(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

            Assert.IsTrue(ChekEventExists(newModel.Name));
        }

        private bool ChekEventExists(string eventName)
        {
            var options = new DbContextOptionsBuilder<EventmiContext>().UseSqlServer("Server=04011987\\SQLEXPRESS;Database=Eventmi;Trusted_Connection=True;MultipleActiveResultSets=true").Options;

            using (var context = new EventmiContext(options))
            {
                return context.Events.Any(e => e.Name == eventName);
            }
        }

        [Test]

        public void GetEventDetails_ReturnsSuccesAndExpectedContent()
        {
            //Arrange

            var eventId = 1;
            var request = new RestRequest($"/Event/Details/{eventId}", Method.Get);

            //Act
            var response = _client.Execute(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK , response.StatusCode);
        }

        [Test]

        public async Task EditAction_ReturnsViewforValidId()
        {
            //Arrange
            var eventId = 1;
            var request = new RestRequest($"/Event/Edit/{eventId}", Method.Get);

            //Act
            var response = await _client.ExecuteAsync(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Test]

        public async Task EditAction_ReturnsSuccessAndEditsEvent()
        {
            //Arrange
            var eventId = 6;
            var eventToEdit = await GetEventByIdAsync(eventId);

            var eventModel = new EventFormModel
            {
                Id = eventToEdit.Id,
                Name = eventToEdit.Name,
                Start = eventToEdit.Start,
                End = eventToEdit.End,
                Place = eventToEdit.Place
            };

            string updatedName = "UpdatedEventName";
            eventModel.Name = updatedName;

            var request = new RestRequest($"/Event/Edit/{eventId}", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("Id", eventModel.Id);
            request.AddParameter("Name", updatedName);
            request.AddParameter("Start", eventModel.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", eventModel.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", eventModel.Place);

            //Act
            var response = await _client.ExecuteAsync(request);
            var eventInDb = await GetEventByIdAsync(eventId);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK , response.StatusCode);
            Assert.AreEqual(eventInDb.Name, updatedName);
          
        }

        private async Task<Event> GetEventByIdAsync(int id)
        {
            var options = new DbContextOptionsBuilder<EventmiContext>().UseSqlServer("Server=04011987\\SQLEXPRESS;Database=Eventmi;Trusted_Connection=True;MultipleActiveResultSets=true").Options;

            using (var context = new EventmiContext(options))
            {
                return await context.Events.FirstOrDefaultAsync(e => e.Id == id);   
            }
        }

        [Test]

        public async Task EditPostAction_MismatchID_ShouldReturnNotFound()
        {

            //Arrange
            var eventId = 6;
            var eventToEdit = await GetEventByIdAsync(eventId);

            var eventModel = new EventFormModel
            {
                Id = 7,
                Name = eventToEdit.Name,
                Start = eventToEdit.Start,
                End = eventToEdit.End,
                Place = eventToEdit.Place
            };


            var request = new RestRequest($"/Event/Edit/{eventId}", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("Id", eventModel.Id);
            request.AddParameter("Name", eventModel.Name);


            //Act
            var response = await _client.ExecuteAsync(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]

        public async Task EditPostAction_WithInvalidModel_ReturnsViweWithModel()
        {
            //Arrange
            var eventId = 6;
            var eventToEdit = await GetEventByIdAsync(eventId);

            var eventModel = new EventFormModel
            {
                Id = eventToEdit.Id,
                Place = eventToEdit.Place
            };


            var request = new RestRequest($"/Event/Edit/{eventId}", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("Id", eventModel.Id);
            request.AddParameter("Name", eventModel.Name);

            //Act
            var response = await _client.ExecuteAsync(request);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

        }

        private Event GetEventByName(string eventName)
        {
            var options = new DbContextOptionsBuilder<EventmiContext>().UseSqlServer("Server=04011987\\SQLEXPRESS;Database=Eventmi;Trusted_Connection=True;MultipleActiveResultSets=true").Options;

            using (var context = new EventmiContext(options))
            {
                return context.Events.FirstOrDefault(e => e.Name == eventName);
            }
        }

        [Test]

        public async Task DeleteAction_WithValidId_ShouldRedirectToAllEvents()
        {
            //Arrange

            var newModel = new EventFormModel
            {
                Name = "Event for Delete",
                Start = new DateTime(2024, 03, 20, 09, 10, 10),
                End = new DateTime(2024, 03, 21, 09, 10, 10),
                Place = "Plovdiv"
            };

            var request = new RestRequest("/Event/Add", Method.Post);

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            request.AddParameter("Name", newModel.Name);
            request.AddParameter("Start", newModel.Start.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("End", newModel.End.ToString("MM/dd/yyyy hh:mm tt"));
            request.AddParameter("Place", newModel.Place);

            _client.Execute(request);

            var eventInDb = GetEventByName(newModel.Name);
            var eventIDForDelete = eventInDb.Id;

            var deleteRequest = new RestRequest($"/Event/Delete/{eventIDForDelete}", Method.Post);
            var response = _client.Execute(deleteRequest);

            //Assert
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }
}