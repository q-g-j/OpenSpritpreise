﻿using ApiClients.Models;
using HttpClient;
using Newtonsoft.Json;
using SettingsFile.SettingsFile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ApiClients
{
    public class TankerkönigClient : IGasPricesClient
    {
        private readonly HttpClientRepository _httpClientRepository;

        public TankerkönigClient(HttpClientRepository httpClientRepository)
        {
            _httpClientRepository = httpClientRepository;
        }

        public async Task<List<Station>?> GetStationsAsync(string apiKey, Coords coords, int radius)
        {
            List<Station>? stations = null;

            string url = $@"https://creativecommons.tankerkoenig.de/json/list.php?lat={coords.Latitude}&lng={coords.Longitude}&rad={radius}&sort=dist&type=all&apikey={apiKey}";
            string result = await _httpClientRepository.GetAsync(url);

            dynamic? jsonObject = JsonConvert.DeserializeObject(result);

            try
            {
                if (jsonObject != null && jsonObject?.stations != null)
                {
                    stations = new List<Station>();
                    var stationsObject = JsonConvert.DeserializeObject<List<dynamic>>(jsonObject?.stations.ToString());
                    foreach (var station in stationsObject)
                    {
                        var stationModel = new Station
                        {
                            Name = station.name,
                            Brand = station.brand,
                            Distance = station.dist,
                            Street = station.street,
                            HouseNumber = station.houseNumber,
                            City = station.place,
                            IsOpen = station.isOpen,
                            PostalCode = station.postCode,
                            E5 = station.e5,
                            E10 = station.e10,
                            Diesel = station.diesel
                        };

                        stations.Add(stationModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return stations;
        }
    }
}
