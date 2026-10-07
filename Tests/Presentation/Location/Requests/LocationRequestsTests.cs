using Presentation.Location.Requests;

namespace Tests.Presentation.Location.Requests;

public class LocationRequestsTests
{
    [Fact]
    public void LocationRequests_CanBeInstantiated()
    {
        var requests = new LocationRequests();

        requests.ShouldNotBeNull();
        requests.ShouldBeOfType<LocationRequests>();
    }
}
