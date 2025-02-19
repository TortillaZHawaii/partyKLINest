﻿using Moq;
using PartyKlinest.ApplicationCore.Entities.Orders;
using PartyKlinest.ApplicationCore.Entities.Orders.Opinions;
using PartyKlinest.ApplicationCore.Entities.Users;
using PartyKlinest.ApplicationCore.Entities.Users.Cleaners;
using PartyKlinest.ApplicationCore.Exceptions;
using PartyKlinest.ApplicationCore.Interfaces;
using PartyKlinest.ApplicationCore.Services;
using PartyKlinest.ApplicationCore.Specifications;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitTests.Factories;
using Xunit;

namespace UnitTests.ApplicationCore.Services.CleanerFacadeTests
{
    public class CheckCleanersPriviliges
    {
        private readonly Mock<IRepository<Order>> _mockOrderRepo = new();
        private readonly Mock<IRepository<Cleaner>> _mockCleanerRepo = new();
        private readonly Mock<IRepository<Client>> _mockClientRepo = new();
        private readonly Mock<IClientService> _mockClientService = new();
        private readonly Mock<IGraphClient> _mockGraphClient = new();

        [Fact]
        public async Task ThrowsWithoutPriviligesExceptionWhenCleanerIsBanned()
        {
            // Arrange
            var cleanerBuilder = new CleanerBuilder();
            var returnedCleaner = cleanerBuilder.Build();
            returnedCleaner.SetCleanerStatus(CleanerStatus.Banned);
            var orderBuilder = new OrderBuilder();
            orderBuilder.WithCleanerId();
            var expected = orderBuilder.Build();
            var newOpinion = new Opinion(4, "New Opinion");
            var cleanerFacade = SetMockRepos(returnedCleaner, expected);

            // Act & Assert
            await Assert.ThrowsAsync<UserWithoutPrivilegesException>(
                () => cleanerFacade.ConfirmOrderCompleted(
                    returnedCleaner.CleanerId,
                    expected.OrderId,
                    newOpinion));
        }

        [Fact]
        public async Task ThrowsWithoutPriviligesExceptionWhenCleanerRateNotOwnOrder()
        {
            // Arrange
            var cleanerBuilder = new CleanerBuilder();
            var returnedCleaner = cleanerBuilder.Build();
            returnedCleaner.SetCleanerStatus(CleanerStatus.Active);
            var orderBuilder = new OrderBuilder();
            orderBuilder.WithCleanerId("it is not returnedCleaner Id");
            var expected = orderBuilder.Build();
            var newOpinion = new Opinion(4, "New Opinion");
            var cleanerFacade = SetMockRepos(returnedCleaner, expected);

            // Act & Assert
            await Assert.ThrowsAsync<UserWithoutPrivilegesException>(
                () => cleanerFacade.ConfirmOrderCompleted(
                    returnedCleaner.CleanerId,
                    expected.OrderId,
                    newOpinion));
        }

        private CleanerFacade SetMockRepos(Cleaner returnedCleaner, Order expected)
        {
            _mockCleanerRepo
                .Setup(x => x.GetByIdAsync(It.IsAny<string>(), default))
                .ReturnsAsync(returnedCleaner);

            _mockCleanerRepo
                .Setup(x => x.ListAsync(It.IsAny<CleanerWithSchedule>(), default))
                .ReturnsAsync(new List<Cleaner> { returnedCleaner });

            _mockOrderRepo
                .Setup(x => x.GetByIdAsync(It.IsAny<long>(), default))
                .ReturnsAsync(expected);

            OrderFacade orderFacade = new(_mockOrderRepo.Object, _mockClientRepo.Object);
            return new CleanerFacade(_mockCleanerRepo.Object, orderFacade, _mockClientService.Object, _mockGraphClient.Object);
        }
    }
}
