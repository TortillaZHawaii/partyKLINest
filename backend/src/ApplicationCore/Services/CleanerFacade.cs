﻿using PartyKlinest.ApplicationCore.Entities.Orders;
using PartyKlinest.ApplicationCore.Entities.Orders.Opinions;
using PartyKlinest.ApplicationCore.Entities.Users.Cleaners;
using PartyKlinest.ApplicationCore.Exceptions;
using PartyKlinest.ApplicationCore.Interfaces;
using PartyKlinest.ApplicationCore.Models;
using PartyKlinest.ApplicationCore.Specifications;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartyKlinest.ApplicationCore.Services
{
    public class CleanerFacade
    {
        public CleanerFacade(IRepository<Cleaner> cleanerRepository, OrderFacade orderFacade,
            IClientService clientService, IGraphClient graphClient)
        {
            _cleanerRepository = cleanerRepository;
            _orderFacade = orderFacade;
            _clientService = clientService;
            _graphClient = graphClient;
        }

        private readonly IRepository<Cleaner> _cleanerRepository;
        private readonly OrderFacade _orderFacade;
        private readonly IClientService _clientService;
        private readonly IGraphClient _graphClient;

        public async Task<List<Order>> GetAssignedOrdersAsync(string cleanerId)
        {
            var cleaner = await GetCleanerInfo(cleanerId);
            return await _orderFacade.ListAssignedOrdersToAsync(cleaner.CleanerId);
        }

        public async Task<Cleaner> GetCleanerInfo(string cleanerId)
        {
            var spec = new CleanerWithSchedule(cleanerId);
            var cleaners = await _cleanerRepository.ListAsync(spec);
            if (cleaners is null || cleaners.Count == 0)
            {
                throw new CleanerNotFoundException(cleanerId);
            }
            return cleaners[0];
        }

        public async Task ConfirmOrderCompleted(string cleanerId, long orderId, Opinion opinion)
        {
            var cleaner = await GetCleanerInfo(cleanerId);
            var order = await _orderFacade.GetOrderAsync(orderId);

            if (CleanerWithoutPrivileges(cleaner, order))
            {
                throw new UserWithoutPrivilegesException(cleaner.CleanerId);
            }

            order.SetCleanersOpinion(opinion);
            _orderFacade.CloseOrder(order);
        }
        private bool CleanerWithoutPrivileges(Cleaner cleaner, Order order)
        {
            return cleaner.Status == CleanerStatus.Banned || (order.CleanerId != cleaner.CleanerId && order.CleanerId != null);
        }


        public async Task AcceptRejectOrder(string cleanerId, Order sentOrder)
        {
            var cleaner = await GetCleanerInfo(cleanerId);
            var localOrder = await _orderFacade.GetOrderAsync(sentOrder.OrderId);
            if (CleanerWithoutPrivileges(cleaner, sentOrder))
            {
                throw new UserWithoutPrivilegesException(cleaner.CleanerId);
            }

            bool wasAsssigned = WasAssignedButNotConfirmed(localOrder, cleanerId);
            bool isAccepted = IsAccepted(sentOrder, cleanerId);
            bool isRejected = IsRejected(sentOrder);

            if (wasAsssigned && (isAccepted || isRejected))
            {
                await _orderFacade.UpdateAsync(sentOrder);
            }
            else
            {
                throw new NotCorrectOrderStatusException(localOrder.Status, sentOrder.Status);
            }
        }
        private bool WasAssignedButNotConfirmed(Order order, string cleanerId)
        {
            return order.Status == OrderStatus.Active && order.CleanerId == cleanerId;
        }
        private bool IsAccepted(Order order, string cleanerId)
        {
            return order.Status == OrderStatus.InProgress && order.CleanerId == cleanerId;
        }
        private bool IsRejected(Order order)
        {
            return order.Status == OrderStatus.Active && order.CleanerId == null;
        }

        public async Task UpdateCleanerAsync(Cleaner updateCleaner)
        {
            var oid = updateCleaner.CleanerId;
            bool clientExists = true;
            try
            {
                await GetCleanerInfo(oid);
            }
            catch (CleanerNotFoundException)
            {
                clientExists = false;
            }

            if (!clientExists)
            {
                await _cleanerRepository.AddAsync(updateCleaner);
            }
            else
            {
                var cleaner = await GetCleanerInfo(updateCleaner.CleanerId);

                if (NeedUpdateStatus(cleaner, updateCleaner))
                {
                    await UpdateStatus(cleaner, updateCleaner);
                }

                if (NeedUpdateOrderFilter(cleaner, updateCleaner))
                {
                    await UpdateOrderFilter(cleaner, updateCleaner);
                }

                if (NeedUpdateSchedule(cleaner, updateCleaner))
                {
                    await UpdateSchedule(cleaner, updateCleaner);
                }
            }
        }
        private async Task UpdateStatus(Cleaner localCleaner, Cleaner updateCleaner)
        {
            localCleaner.SetCleanerStatus(updateCleaner.Status);
            await _cleanerRepository.UpdateAsync(localCleaner);
        }
        private async Task ActivateCleaner(Cleaner localCleaner)
        {
            localCleaner.SetCleanerStatus(CleanerStatus.Active);
            await _cleanerRepository.UpdateAsync(localCleaner);
        }
        private async Task UpdateOrderFilter(Cleaner localCleaner, Cleaner updateCleaner)
        {
            if (localCleaner.Status == CleanerStatus.Registered)
            {
                await ActivateCleaner(localCleaner);
            }
            if (localCleaner.Status == CleanerStatus.Active)
            {
                localCleaner.UpdateOrderFilter(updateCleaner.OrderFilter);
                await _cleanerRepository.UpdateAsync(localCleaner);
            }
            else
            {
                throw new UserNotActiveException(localCleaner);
            }
        }
        private async Task UpdateSchedule(Cleaner localCleaner, Cleaner updateCleaner)
        {
            if (localCleaner.Status == CleanerStatus.Registered)
            {
                await ActivateCleaner(localCleaner);
            }
            if (localCleaner.Status == CleanerStatus.Active)
            {
                localCleaner.UpdateSchedule(updateCleaner.ScheduleEntries);
                await _cleanerRepository.UpdateAsync(localCleaner);
            }
            else
            {
                throw new UserNotActiveException(localCleaner);
            }
        }
        private bool NeedUpdateStatus(Cleaner localCleaner, Cleaner updateCleaner)
        {
            if (localCleaner.Status == CleanerStatus.Banned ||
                updateCleaner.Status == CleanerStatus.Banned)
            {
                throw new CleanerCannotChangeBannedStatusException(localCleaner, updateCleaner);
            }
            return localCleaner.Status != updateCleaner.Status;
        }
        private bool NeedUpdateOrderFilter(Cleaner localCleaner, Cleaner sentCleaner)
        {
            return localCleaner.OrderFilter != sentCleaner.OrderFilter;
        }
        private bool NeedUpdateSchedule(Cleaner localCleaner, Cleaner updateCleaner)
        {
            if (localCleaner.ScheduleEntries.Count != updateCleaner.ScheduleEntries.Count)
                return true;
            for (int i = 0; i < localCleaner.ScheduleEntries.Count; i++)
            {
                if (localCleaner.ScheduleEntries.ElementAt(i) != updateCleaner.ScheduleEntries.ElementAt(i))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<List<Cleaner>> ListCleanersMatchingOrderAsync(long orderId)
        {
            var order = await _orderFacade.GetOrderAsync(orderId);

            double? clientRating = await _clientService.GetAverageClientRatingAsync(order.ClientId);

            var date = order.Date;
            var spec = new CleanersMatchingOrderSpecification(date, order.MessLevel, order.MaxPrice,
                clientRating);

            return await _cleanerRepository.ListAsync(spec);
        }

        public async Task<List<UserInfo>> ListCleanersMatchingOrderAsUsersAsync(long orderId)
        {
            var cleaners = await ListCleanersMatchingOrderAsync(orderId);
            var ids = cleaners.Select(c => c.CleanerId).ToList();
            return await _graphClient.GetUserInfoAsync(ids);
        }
    }
}
