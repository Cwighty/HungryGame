﻿using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace foolhearty;

public abstract class BasePlayerLogic : IPlayerLogic
{
    protected HttpClient httpClient = new();
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

    protected virtual string turn(string direction) => direction switch
    {
        "up" => "right",
        "right" => "down",
        "down" => "left",
        "left" => "up",
        _ => throw new Exception("What sort of direction are you trying to go?")
    };

    protected virtual private string tryNextDirection(string direction, List<Cell> board, Location currentLocation)
    {
        var possibleDirections = new List<string>(new[] { "up", "right", "down", "left" });
        possibleDirections.Remove(direction);
        if (currentLocation.column == 0)
            possibleDirections.Remove("left");
        else if (currentLocation.row == 0)
            possibleDirections.Remove("up");
        else if (currentLocation.column == board.Max(c => c.location.column))
            possibleDirections.Remove("right");
        else if (currentLocation.row == board.Max(c => c.location.row))
            possibleDirections.Remove("down");

        var availableNeighbors = new Dictionary<Location, Cell>(board.Where(c => c.location.column >= currentLocation.column - 1 && c.location.column <= currentLocation.column + 1 &&
                                                  c.location.row >= currentLocation.row - 1 && c.location.row <= currentLocation.row + 1 &&
                                                  c.occupiedBy == null).Select(c => new KeyValuePair<Location, Cell>(c.location, c)));
        var left = new Location(currentLocation.row, currentLocation.column - 1);
        var right = new Location(currentLocation.row, currentLocation.column + 1);
        var up = new Location(currentLocation.row - 1, currentLocation.column);

        if (possibleDirections.Contains("left") && availableNeighbors.ContainsKey(left))
            return "left";
        if (possibleDirections.Contains("right") && availableNeighbors.ContainsKey(right))
            return "right";
        if (possibleDirections.Contains("up") && availableNeighbors.ContainsKey(up))
            return "up";
        return "down";
    }

    protected virtual Location acquireTarget(Location curLocation, List<Cell> board)
    {
        var max = new Location(int.MaxValue, int.MaxValue);

        Location closest = findClosestPillToEat(curLocation, board, max);

        if (closest == max)//e.g. didn't find a pill to eat...look for another player
        {
            closest = findNearestPlayerToAttack(curLocation, board, max, closest);
        }

        return closest;
    }

    protected virtual Location findClosestPillToEat(Location curLocation, List<Cell> board, Location max)
    {
        var closest = max;
        var minDistance = double.MaxValue;

        foreach (var cell in board)
        {
            if (!cell.isPillAvailable)
            {
                continue;
            }
            var a = curLocation.row - cell.location.row;
            var b = curLocation.column - cell.location.column;
            var newDistance = Math.Sqrt((a * a) + (b * b));
            if (newDistance < minDistance)
            {
                minDistance = newDistance;
                closest = cell.location;
            }
        }

        return closest;
    }

    protected virtual Location findNearestPlayerToAttack(Location curLocation, List<Cell> board, Location max, Location closest)
    {
        var minScore = int.MaxValue;
        foreach (var cell in board)
        {
            if (cell.occupiedBy == null || cell.location == curLocation)
            {
                continue;
            }
            if (cell.occupiedBy.score < minScore)
            {
                minScore = cell.occupiedBy.score;
                closest = cell.location;
            }
        }

        return closest;
    }

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
            gameState = await httpClient.GetStringAsync($"{url}/state", cancellationToken);
        }
    }

    protected async Task<List<Cell>> getBoardAsync()
    {
        var boardString = await httpClient.GetStringAsync($"{url}/board");
        return JsonSerializer.Deserialize<IEnumerable<Cell>>(boardString)?.ToList() ?? throw new MissingBoardException();
    }
}
