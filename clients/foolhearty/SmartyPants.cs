﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace foolhearty;

public class SmartyPants : BasePlayerLogic
{
    private readonly ILogger<SmartyPants> logger;
    private Dictionary<Location, Cell> map;
    private List<Cell> board;

    public SmartyPants(IConfiguration config, ILogger<SmartyPants> logger) : base(config)
    {
        this.logger = logger;
    }

    public override string PlayerName => "SmartyPants";

    public override async Task PlayAsync(CancellationTokenSource cancellationTokenSource)
    {
        logger.LogInformation("SmartyPants starting to play");

        var timer = new Timer(getBoard, null, 0, 1_000);

        var direction = "right";
        var moveResult = new MoveResult { newLocation = new Location(0, 0) };
        while (true)
        {
            await refreshBoardAndMap();
            var destination = acquireTarget(moveResult?.newLocation, board);

            direction = inferDirection(moveResult?.newLocation, destination);
            moveResult = await httpClient.GetFromJsonAsync<MoveResult>($"{url}/move/{direction}?token={token}");
            if (moveResult?.ateAPill == false)
            {
                logger.LogInformation("Didn't eat a pill...keep searching.  Move from {from} to {destination}", moveResult.newLocation, destination);
                moveResult = await moveFromTo(moveResult, destination);
                logger.LogInformation("   moveResult={moveResult}", moveResult);
                continue;
            }
            var nextLocation = advance(moveResult?.newLocation, direction);
            Task<MoveResult> lastRequest = null;
            while (map.ContainsKey(nextLocation) && map[nextLocation].isPillAvailable)
            {
                logger.LogInformation("In a groove!  Keep going!");
                lastRequest = httpClient.GetFromJsonAsync<MoveResult>($"{url}/move/{direction}?token={token}");
                nextLocation = advance(nextLocation, direction);
            }
            if (lastRequest != null)
            {
                logger.LogInformation("Wait for response from most recent request");
                moveResult = await lastRequest;
            }
        }
    }

    protected override Location findNearestPlayerToAttack(Location curLocation, List<Cell> board, Location max, Location closest)
    {
        var nearestPlayer = base.findNearestPlayerToAttack(curLocation, board, max, closest);
        var columnDelta = nearestPlayer.column - curLocation.column;
        var rowDelta = nearestPlayer.row - curLocation.row;
        return new Location(curLocation.row + (rowDelta * -1), curLocation.column + (columnDelta * -1));
    }

    private async Task refreshBoardAndMap()
    {
        board = await getBoardAsync();
        map = new Dictionary<Location, Cell>(board.Select(c => new KeyValuePair<Location, Cell>(c.location, c)));
    }

    private Location advance(Location? lastLocation, string direction)
    {
        return direction switch
        {
            "left" => lastLocation with { column = lastLocation.column - 1 },
            "right" => lastLocation with { column = lastLocation.column + 1 },
            "up" => lastLocation with { row = lastLocation.row - 1 },
            "down" => lastLocation with { row = lastLocation.row + 1 },
            _ => lastLocation
        };
    }

    private async void getBoard(object _)
    {
        var newBoard = await getBoardAsync();
        var newMap = new Dictionary<Location, Cell>(newBoard.Select(c => new KeyValuePair<Location, Cell>(c.location, c)));
        Interlocked.Exchange(ref board, newBoard);
        Interlocked.Exchange(ref map, newMap);
    }

    private async Task<MoveResult> moveFromTo(MoveResult current, Location destination)
    {
        var rowDelta = destination.row - current.newLocation.row;
        var colDelta = destination.column - current.newLocation.column;

        var direction = rowDelta < 0 ? "up" : "down";
        Task<MoveResult> result = null;
        for (int i = 0; i < Math.Abs(rowDelta); i++)
        {
            result = httpClient.GetFromJsonAsync<MoveResult>($"{url}/move/{direction}?token={token}");
        }

        direction = colDelta < 0 ? "left" : "right";
        for (int i = 0; i < Math.Abs(colDelta); i++)
        {
            result = httpClient.GetFromJsonAsync<MoveResult>($"{url}/move/{direction}?token={token}");
        }

        if (result != null)
            return await result;

        return new MoveResult { newLocation = destination };
    }
}

public class MoveResult
{
    public Location newLocation { get; set; }
    public bool ateAPill { get; set; }
}

public static class Extensions
{
}
