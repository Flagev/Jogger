﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Jogger.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Jogger.IO;
using Jogger.Communication;
using System.Threading.Tasks;

namespace Jogger.Services.Tests
{
    [TestClass()]
    public class TesterServiceTests
    {
        [TestMethod()]
        public void Initialize_InitializationSuccess_SetsStateInitialized()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.OK, "OK", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            ActionStatus actionStatus = testerService.Initialize(new ConfigurationSettings());
            Assert.AreEqual(ProgramState.Initialized, testerService.State);
        }
        [TestMethod()]
        public void Initialize_InitializationSuccess_ReturnsActionStatusOk()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.OK, "OK", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            ActionStatus status = testerService.Initialize(new ConfigurationSettings());
            Assert.AreEqual(ActionStatus.OK, status);
        }
        [TestMethod()]
        public void Initialize_InitializationFailed_SetsStateError()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.Error, "Error", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })) { };
            ActionStatus status = testerService.Initialize(new ConfigurationSettings());
            Assert.AreEqual(ProgramState.Error, testerService.State);
        }
        [TestMethod()]
        public void Initialize_InitializationFailed_ReturnsActionStatusError()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.Error, "Error", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })) { };
            ActionStatus status = testerService.Initialize(new ConfigurationSettings());
            Assert.AreEqual(ActionStatus.Error, status);
        }
        [TestMethod()]
        public void Start_ExecutedOk_SetsStateStarted()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.OK, "Ok", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })) { };
            Func<TestSettings, string, ActionStatus> startFunc = (TestSettings testSettings, string text) => { return ActionStatus.OK; };
            ActionStatus status = testerService.Start(startFunc, new TestSettings());
            Assert.AreEqual(ProgramState.Started, testerService.State);
        }
        [TestMethod()]
        public void Start_ExecutedOk_SetsStateError()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.Error, "Error", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })) { };
            Func<TestSettings, string, ActionStatus> startFunc = (TestSettings testSettings, string text) => { return ActionStatus.Error; };
            ActionStatus status = testerService.Start(startFunc, new TestSettings());
            Assert.AreEqual(ProgramState.Error, testerService.State);
        }
        [TestMethod()]
        public void Stop_Stopped_StopFuncExecuted()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.Error, "Error", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })) { };
            bool executionDone = false;
            testerService.Stop(() => executionDone = true);
            Assert.AreEqual(true, executionDone);
        }
        [TestMethod()]
        public void Stop_Stopped_SetsStateIdle()
        {
            TesterService testerService = new TesterService(new CommunicationStub(), new DigitalIOStub(ActionStatus.Error, "Error", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 })) { };
            testerService.Stop(() => { });
            Assert.AreEqual(ProgramState.Idle, testerService.State);
        }

        [TestMethod()]
        public void Dispose_OnNulls_DoesntThrowNullReferenceException()
        {
            TesterService testerService = new TesterService(null,null) { };
            testerService.Dispose();
            Assert.IsTrue(true);
        }
    }
}