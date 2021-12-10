using FluentAssertions;
using Gherkin;
using HungryHippos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;

namespace HungryTests.StepDefinitions
{
    [Binding]
    public class GameInfoTestsStepDefinitions
    {
        private readonly ScenarioContext context;
        private const string SECRET_CODE = nameof(SECRET_CODE);
        private static int lastRandom = -1;
        private readonly Queue<Direction> moves = new(new[]
        {
            Direction.Left,
            Direction.Down,
            Direction.Right
        });

        public GameInfoTestsStepDefinitions(ScenarioContext context)
        {
            this.context = context;
        }

        private GameLogic getGame()
        {
            if (context.TryGetValue(out GameLogic game) is false)
            {
                var configMock = new Mock<IConfiguration>();
                configMock.Setup(m => m["SECRET_CODE"]).Returns(SECRET_CODE);
                var loggerMock = new Mock<ILogger<GameLogic>>();
                var randomMock = new Mock<IRandomService>();
                randomMock.Setup(m => m.Next(It.IsAny<int>())).Returns(() =>
                {
                    lastRandom++;
                    if(lastRandom >= 2)
                        lastRandom = 0;
                    return lastRandom;
                });
                game = new GameLogic(configMock.Object, loggerMock.Object, randomMock.Object);
                context.Set(game);
            }
            return game;
        }

        [Given(@"(.*) joins")]
        [When(@"(.*) joins")]
        public void GivenPlayerJoins(string playerName)
        {
            var game = getGame();
            var token = game.JoinPlayer(playerName);
            context.Add(playerName, token);
        }

        [Given(@"(.*) players join the game")]
        public void GivenPlayersJoinTheGame(int numberOfPlayers)
        {
            var game = getGame();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                game.JoinPlayer(numberOfPlayers.ToString());
            }
        }

        [Then(@"starting a game with (.*) rows, (.*) columns gives a (.*) exeption\.")]
        public void ThenStartingAGameWithRowsColumnsGivesATooManyPlayersExeption_(int rows, int cols, string exceptionMessage)
        {
            var game = getGame();
            try
            {
                game.StartGame(rows, cols, SECRET_CODE);
                Assert.Fail("Should never make it here");
            }
            catch (Exception e)
            {
                e.Message.Should().ContainEquivalentOf(exceptionMessage);
            }
        }

        [Given(@"the game starts with (.*) rows, (.*) columns")]
        public void GivenTheGameStarts(int numRows, int numColumns)
        {
            var game = getGame();
            game.StartGame(numRows, numColumns, SECRET_CODE);
        }

        [When(@"(.*) eats a pill")]
        [When(@"(.*) eats another pill")]
        public void WhenPlayerEatsAPill(string playerName)
        {
            var game = getGame();
            var token = context.Get<string>(playerName);
            MoveResult result = game.Move(token, moves.Dequeue());
            context.Set(result);
        }

        [When(@"(.*) moves (.*)")]
        public void WhenPMovesLeft(string playerName, string direction)
        {
            var game = getGame();
            var token = context.Get<string>(playerName);
            var parsedDirection = Enum.Parse<Direction>(direction);
            var moveResult = game.Move(token, parsedDirection);
            context.Set(moveResult);
        }


        [Then(@"(.*)'s score is (.*)")]
        public void ThenPlayersScoreIs(string playerName, int score)
        {
            var game = getGame();
            var players = game.GetPlayersByScoreDescending();
            players.First(p => p.Name == playerName).Score.Should().Be(score);
        }

        [Then(@"their new location is \((.*),(.*)\)")]
        public void ThenPlayersLocationIs(int row, int col)
        {
            var moveResult = context.Get<MoveResult>();
            moveResult.NewLocation.Should().Be(new Location(row, col));
        }

        [Then(@"they did eat a pill")]
        public void ThenTheyDidEatAPill()
        {
            var moveResult = context.Get<MoveResult>();
            moveResult.AteAPill.Should().BeTrue();
        }

        [Then(@"(.*) gets a valid token")]
        public void Thenplayergetsavalidtoken(string playerName)
        {
            var token = context.Get<string>(playerName);
            token.Should().NotBeNullOrEmpty();
        }

        [Then(@"there are two players")]
        public void Giventherearetwoplayers()
        {
            var game = getGame();
            game.GetPlayersByScoreDescending().Count().Should().Be(2);
        }

        [Then(@"(.*) cannot join because (.*)")]
        public void ThenPlayerCannotJoinBecauseThereIsNoAvailableSpace(string playerName, string errorMessage)
        {
            var game = getGame();
            try
            {
                game.JoinPlayer(playerName);
                Assert.Fail("Should never make it here");
            }
            catch(Exception ex)
            {
                ex.Message.Should().ContainEquivalentOf(errorMessage);
            }
        }

        [Then(@"the game state is (.*)")]
        [Given(@"the game state is (.*)")]
        public void ThenTheGamestateIs___(string gameState)
        {
            var game = getGame();
            var expectedGameState = Enum.Parse<GameState>(gameState);
            game.CurrentGameState.Should().Be(expectedGameState);
        }

        [Then(@"(.*)'s location is \((.*),(.*)\)")]
        public void ThenPsLocationIs(string playerName, int row, int col)
        {
            var game = getGame();
            var boardState = game.GetBoardState();
            var playerCell = boardState.FirstOrDefault(c => c.OccupiedBy?.Name == playerName);
            if (playerCell == null)
                throw new ApplicationException("Unable to find player cell");
            playerCell.Location.Row.Should().Be(row);
            playerCell.Location.Column.Should().Be(col);
        }

        [Then(@"(.*) is removed from the board")]
        public void ThenPIsRemovedFromTheBoard(string playerName)
        {
            var game = getGame();
            var boardState = game.GetBoardState();
            var playerCell = boardState.FirstOrDefault(c => c.OccupiedBy?.Name == playerName);
            playerCell.Should().BeNull();
        }

        [Then(@"(.*) is declared winner")]
        public void ThenPIsDeclaredWinner(string playerName)
        {
            var game = getGame();
            game.CurrentGameState.Should().Be(GameState.GameOver);
            game.GetPlayersByScoreDescending().First().Name.Should().Be(playerName);
        }

    }
}
