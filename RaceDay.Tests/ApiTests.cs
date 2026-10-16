using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;


public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

  

    private static string NewEmail() => $"{Guid.NewGuid():N}@test.co.za";

   
    private async Task<HttpClient> LoggedInClient(string role)
    {
        var client = _factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/Auth/register",
            new { fullName = "Test " + role, email, password = "Password123", role });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/Auth/login",
            new { email, password = "Password123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return client;
    }

    private static async Task<int> ReadId(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }

    private static object EventBody(string name = "Test Run") => new
    {
        eventName = name,
        eventType = "run",
        eventDate = "2026-12-05",
        location = "Soweto",
        description = "Test event",
        distanceKM = 10
    };

    private async Task<(int eventId, int categoryId)> CreateEventWithCategory(HttpClient organiser)
    {
        var ev = await organiser.PostAsJsonAsync("/api/Events", EventBody());
        Assert.Equal(HttpStatusCode.Created, ev.StatusCode);
        var eventId = await ReadId(ev, "eventID");

        var cat = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "10km", distanceKM = 10, entryFee = 150, maxParticipants = 500 });
        Assert.Equal(HttpStatusCode.Created, cat.StatusCode);

        return (eventId, await ReadId(cat, "categoryID"));
    }


    [Fact]
    public async Task Register_WithValidDetails_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/register",
            new { fullName = "New User", email = NewEmail(), password = "Password123", role = "participant" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        var body = new { fullName = "Dup User", email, password = "Password123", role = "participant" };

        await client.PostAsJsonAsync("/api/Auth/register", body);
        var second = await client.PostAsJsonAsync("/api/Auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/register",
            new { fullName = "Bad Role", email = NewEmail(), password = "Password123", role = "admin" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOkAndRole()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/Auth/register",
            new { fullName = "Login User", email, password = "Password123", role = "organiser" });

        var login = await client.PostAsJsonAsync("/api/Auth/login",
            new { email, password = "Password123" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("organiser", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/Auth/register",
            new { fullName = "Wrong Pw", email, password = "Password123", role = "participant" });

        var login = await client.PostAsJsonAsync("/api/Auth/login",
            new { email, password = "WrongPass" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Auth/login",
            new { email = NewEmail(), password = "Password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }



    [Fact]
    public async Task GetEvents_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Events");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await LoggedInClient("organiser");
        var response = await organiser.PostAsJsonAsync("/api/Events", EventBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_WithInvalidType_ReturnsBadRequest()
    {
        var organiser = await LoggedInClient("organiser");
        var response = await organiser.PostAsJsonAsync("/api/Events", new
        {
            eventName = "Bad Type",
            eventType = "swim",
            eventDate = "2026-12-05",
            location = "Soweto",
            description = "x",
            distanceKM = 5
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await LoggedInClient("organiser");
        var created = await organiser.PostAsJsonAsync("/api/Events", EventBody());
        var id = await ReadId(created, "eventID");

        var update = await organiser.PutAsJsonAsync($"/api/Events/{id}", EventBody("Renamed Run"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var fetched = await organiser.GetFromJsonAsync<JsonElement>($"/api/Events/{id}");
        Assert.Equal("Renamed Run", fetched.GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await LoggedInClient("organiser");
        var created = await organiser.PostAsJsonAsync("/api/Events", EventBody());
        var id = await ReadId(created, "eventID");

        var delete = await organiser.DeleteAsync($"/api/Events/{id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        var after = await organiser.GetAsync($"/api/Events/{id}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var owner = await LoggedInClient("organiser");
        var created = await owner.PostAsJsonAsync("/api/Events", EventBody());
        var id = await ReadId(created, "eventID");

        var otherOrganiser = await LoggedInClient("organiser");
        var update = await otherOrganiser.PutAsJsonAsync($"/api/Events/{id}", EventBody("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var participant = await LoggedInClient("participant");
        var response = await participant.PostAsJsonAsync("/api/Events", EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var participant = await LoggedInClient("participant");
        var response = await participant.GetAsync("/api/Events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

 

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await LoggedInClient("organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("participant");

        var enrol = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);


        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/my");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(categoryId, mine[0].GetProperty("categoryID").GetInt32());
        Assert.Equal(eventId, mine[0].GetProperty("eventID").GetInt32());

        var forOrganiser = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, forOrganiser.GetArrayLength());
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameCategory()
    {
        var organiser = await LoggedInClient("organiser");
        var (_, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("participant");

        await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var second = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol()
    {
        var organiser = await LoggedInClient("organiser");
        var (_, categoryId) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrol", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments()
    {
        var organiser = await LoggedInClient("organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("participant");

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

  

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var organiser = await LoggedInClient("organiser");
        var (_, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("participant");

        var enrol = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var enrolmentId = await ReadId(enrol, "enrolmentID");

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:52:30", positionOverall = 12, positionCategory = 3 });
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var results = await participant.GetFromJsonAsync<JsonElement>("/api/results/my");
        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal(12, results[0].GetProperty("positionOverall").GetInt32());
    }

    [Fact]
    public async Task Participant_CannotCaptureResult()
    {
        var organiser = await LoggedInClient("organiser");
        var (_, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await LoggedInClient("participant");

        var enrol = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var enrolmentId = await ReadId(enrol, "enrolmentID");

        var capture = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:52:30", positionOverall = 1, positionCategory = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, capture.StatusCode);
    }
}