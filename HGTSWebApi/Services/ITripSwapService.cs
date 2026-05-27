using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HGTSWebApi.DTOs;

namespace HGTSWebApi.Services
{
    public interface ITripSwapService
    {
        Task<TripSwapResponseDto> SwapTripAsync(Guid originalTripId, TripSwapRequestDto request);
        Task<bool> EmergencyEndTripAsync(Guid tripId, EmergencyEndRequestDto request);
        Task<List<TripTransferDto>> GetTripTransfersAsync(Guid tripId);
        Task<ActiveTripWithSwapInfoDto> GetActiveTripWithSwapInfoAsync(string deviceId);
        Task<bool> IsTripSwappedAsync(Guid tripId);
    }
}