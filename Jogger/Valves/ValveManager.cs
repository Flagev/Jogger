﻿using Jogger.Drivers;
using Jogger.IO;
using Jogger.Models;
using Jogger.Services;
using Jogger.ValveTypes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using static Jogger.Valves.IValveManager;

namespace Jogger.Valves
{
    public class ValveManager : IValveManager
    {
        int actualProcessedChannel = 0;
        int previousProcessedChannel = 0;
        public List<Valve> valves = new List<Valve>();
        readonly static string namespacePrefix;
        private IDriver driver;
        IDigitalIO digitalIO;
        private TestSettings testSettings;
        public bool IsTestingDone { get; set; } = false;
        public event CommunicationLogEventHandler CommunicationLogChanged;      
        public event ErrorsEventHandler ActiveErrorsChanged;
        public event ErrorsEventHandler OccuredErrorsChanged;
        public event ResultEventHandler ResultChanged;
        static ValveManager()
        {
            namespacePrefix = System.Reflection.Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace;
            namespacePrefix += ".";
        }
        public ValveManager(IDigitalIO digitalIO, IDriver driver, TestSettings testSettings)
        {
            this.testSettings = testSettings;
            this.driver = driver;
            this.digitalIO = digitalIO;
            digitalIO.InputsRead += DigitalIO_InputsRead;
        }
        public void SetTestSettings(TestSettings testSettings) 
        {
            this.testSettings = testSettings;
        }
        public ActionStatus Initialize(int channelsCount)
        {
            for (int i = 0; i < channelsCount; i++)
            {
                valves.Add(new Valve(driver));//TOD
                valves[i].OccuredErrorsChanged += Receiver_OccuredErrorsChanged;
                valves[i].ActiveErrorsChanged += Receiver_ActiveErrorsChanged;
                valves[i].ResultChanged += ValveManager_ResultChanged;
            }
            return ActionStatus.OK;
        }
        private void ValveManager_ResultChanged(object sender, Result result, int channelNumber)
        {
            bool testDoneCheck = true;
            foreach (Valve valve in valves)
            {
                if (valve.Result == Result.Idle | valve.Result == Result.Testing) testDoneCheck = false;
            }
            IsTestingDone = testDoneCheck;
            ResultChanged?.Invoke(sender, result, channelNumber);
        }
        public ActionStatus Start(TestSettings testSettings, string valveTypeTxt)
        {
            this.testSettings = testSettings;
            Valve.testSettings = testSettings;
            ValveType valveType = GetValveType(valveTypeTxt);
            if (valveType is null) return ActionStatus.Error;
            else
            {
                foreach (Valve valve in valves)
                {
                    valve.Queries = valveType.queryList;
                    valve.errorCodes = valveType.errorCodes;
                    valve.Start();
                }
                IsTestingDone = false;
                return ActionStatus.OK;
            }
        }
        ValveType GetValveType(string valveTypeTxt)
        {
            string namespacePrefix = System.Reflection.Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace;

            Type valveType = Type.GetType(namespacePrefix + ".ValveTypes.ValveType" + valveTypeTxt, false);
            if (valveType is null)
            {
                Trace.WriteLine("ValveType could not be created !");
                return null;
            }
            else
            {
                return Activator.CreateInstance(valveType) as ValveType;
            }
        }
        public void Stop()
        {
            foreach (Valve valve in valves)
            {
                valve.IsStopRequested = true;
            }
        }
        public async Task SendData()
        {
            if (valves[actualProcessedChannel].IsStarted)
            {
                string dataToDriver = await valves[actualProcessedChannel].ExecuteStep(driver.MasterMask[actualProcessedChannel]);
                if (Valve.testSettings.IsLogOutDataSelected)
                {
                    CommunicationLogChanged?.Invoke(this, dataToDriver+"\n");
                }
            }
        }
        public async Task ReceiveData()
        {
            string dataFromDriver = await valves[actualProcessedChannel].Receive();
            if (testSettings.IsLogInDataSelected &(testSettings.IsLogTimeoutSelected | !dataFromDriver.Equals("Timeout")))
            {
                CommunicationLogChanged?.Invoke(this, dataFromDriver+"\n");
            }
        }
        public void SetNextProcessedChannel()
        {
            previousProcessedChannel = actualProcessedChannel;
            while (true)
            {
                actualProcessedChannel++;
                if (actualProcessedChannel >= valves.Count)
                {
                    actualProcessedChannel = 0;
                }
                if (valves[actualProcessedChannel].IsStarted |
                    actualProcessedChannel == previousProcessedChannel)
                {
                    break;
                }
            }
        }
        private void Receiver_ActiveErrorsChanged(object sender, string errors, int channelNumber)
        {
            ActiveErrorsChanged?.Invoke(sender, errors, channelNumber);
        }
        private void Receiver_OccuredErrorsChanged(object sender, string errors, int channelNumber)
        {
            OccuredErrorsChanged?.Invoke(sender, errors, channelNumber);
        }
        private void DigitalIO_InputsRead(object sender, string errorCode, byte[] buffer)
        {
            foreach (Valve valve in valves)
            {
                valve.IsInflated = (buffer[0] & (1 << valve.ChannelNumber * 2)) != 0;
                valve.IsDeflated = (buffer[0] & (1 << valve.ChannelNumber * 2 + 1)) != 0;
            }
        }
    }
}