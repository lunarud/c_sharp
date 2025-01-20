using System;
using AMPS.Client;

public class AmpsConnectivityTester
{
    private readonly string _ampsUri;

    public AmpsConnectivityTester(string ampsUri)
    {
        _ampsUri = ampsUri;
    }

    public bool TestConnectivity()
    {
        using var client = new HAClient("TestClient");
        try
        {
            // Attempt to connect to the AMPS server
            client.Connect(_ampsUri);

            // Verify that the connection is live
            Console.WriteLine("Connected to AMPS successfully.");
            return true;
        }
        catch (AMPS.Client.Exceptions.ConnectionException ex)
        {
            // Handle connection exceptions
            Console.WriteLine($"Connection to AMPS failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            // Always ensure the client disconnects
            client.Disconnect();
        }

        return false;
    }
}
