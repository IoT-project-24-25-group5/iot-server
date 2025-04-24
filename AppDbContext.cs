using System.Text.Json;
using iot_server_cs.Controllers;

namespace iot_server_cs;

using Microsoft.EntityFrameworkCore;


public class SensorData
{
    public required string Id { get; set; }
    public required string Value { get; set; }
}

public class FullDataBaseDto
{
    public required List<SensorData> sensors { get; set; }
    public required LocationDto location { get; set; }
    public required double locationrange { get; set; }
    public required LocationDto center_allowed_location { get; set; }
    public required bool redlight { get; set; }
}



public class AppDbContext : DbContext
{
    
    private static LocationDto Location = new LocationDto{latitude = 51.184, longitude = 4.42};

    private static double locationrange = 100;
    
    private static LocationDto center_allowed_location = new LocationDto { latitude = 51.184, longitude = 4.42 };
    
    private static bool redlight = true;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    private DbSet<SensorData> Sensors { get; set; }

    public FullDataBaseDto GetDbDto()
    {
        return new FullDataBaseDto
        {
            sensors = Sensors.ToList(),
            location = Location,
            locationrange = locationrange,
            center_allowed_location = center_allowed_location,
            redlight = redlight
        };
    }

    private void SendSocketUpdate()
    {
        WebsocketStore.SendToClients(JsonSerializer.Serialize(GetDbDto()));
    }
    
    public void AddSensorData(string id, string value)
    {
        Sensors.Add(new SensorData
        {
            Id = id,
            Value = value
        });
        SaveChanges();
        SendSocketUpdate();
    }

    public List<SensorData> GetSensors()
    {
        return Sensors.ToList();
    }
    
    public double Haversine(LocationDto loc1, LocationDto loc2)
    {
        double R = 6371e3; // metres
        double lat1 = loc1.latitude * Math.PI / 180;
        double lat2 = loc2.latitude * Math.PI / 180;
        double deltaLat = (loc2.latitude - loc1.latitude) * Math.PI / 180;
        double deltaLon = (loc2.longitude - loc1.longitude) * Math.PI / 180;

        double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c; // in metres
    }

    public void CheckLoacation()
    {
        double distance = Haversine(center_allowed_location, Location);
        if (distance > locationrange)
        {
            redlight = true;
        }
        else
        {
            redlight = false;
        }
        
        if (WebsocketStore.pytrack != null)
        {
            if (redlight)
            {
                WebsocketStore.sendText(WebsocketStore.pytrack, "redlight");
            }
            else
            {
                WebsocketStore.sendText(WebsocketStore.pytrack,"nolight");
            }
        }
    }

    public void SetLocationRange(double range)
    {
        locationrange = range;
        SaveChanges();
        CheckLoacation();
        SendSocketUpdate();
    }
    
    public void SetLocationRangeCenter(double lat, double lon)
    {
        center_allowed_location.latitude = lat;
        center_allowed_location.longitude = lon;
        SaveChanges();
        CheckLoacation();
        SendSocketUpdate();
    }
    
    public void UpdateLocation(double lat, double lon)
    {
        Location.latitude = lat;
        Location.longitude = lon;
        SaveChanges();
        CheckLoacation();
        SendSocketUpdate();
    }
    
    public void SetLocation(LocationDto location)
    {
        Location = location;
        SaveChanges();
        CheckLoacation();
        SendSocketUpdate();
    }

    public LocationDto GetLocation()
    {
        return Location;
    }
}