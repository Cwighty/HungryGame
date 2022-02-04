﻿using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace foolhearty;

public abstract class BasePlayerLogic : IPlayerLogic
{
    protected HttpClient httpClient = new HttpClient();
    protected string url = "";
    protected string? token;
    protected readonly IConfiguration config;

    protected BasePlayerLogic(IConfiguration config)
    {
        this.config = config;
    }

    public abstract string PlayerName { get; }

    public virtual async Task JoinGameAsync()
    {
        url = config["SERVER"] ?? "https://hungrygame.azurewebsites.net";
        token = await httpClient.GetStringAsync($"{url}/join?playerName={PlayerName}");
    }

    public abstract Task PlayAsync(CancellationTokenSource cancellationTokenSource);


    protected async Task<bool> checkIfGameOver()
    {
        return (await httpClient.GetStringAsync($"{url}/state")) == "GameOver";
    }

    protected virtual private string tryNextDirection(string direction) => direction switch
    {
        "down" => "left",
        "left" => "up",
        "up" => "right",
        "right" => "down"
    };

    protected static string inferDirection(Location currentLocation, Location destination)
    {
        if (currentLocation.row < destination.row)
        {
            return "down";
        }
        else if (currentLocation.row > destination.row)
        {
            return "up";
        }

        if (currentLocation.column < destination.column)
        {
            return "right";
        }
        return "left";
    }
    protected async Task waitForGameToStart(CancellationToken cancellationToken)
    {
        var gameState = await httpClient.GetStringAsync($"{url}/state");
        while (gameState == "Joining" || gameState == "GameOver")
        {
            await Task.Delay(2_000, cancellationToken);
            gameState = await httpClient.GetStringAsync($"{url}/state");
        }
    }

    protected async Task<List<Cell>> getBoardAsync()
    {
        var boardString = await httpClient.GetStringAsync($"{url}/board");
        return JsonSerializer.Deserialize<IEnumerable<Cell>>(boardString).ToList();
    }
}
