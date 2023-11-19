﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HomeSeer.PluginSdk;
using HomeSeer.PluginSdk.Devices;
using Hspi;
using Hspi.Device;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HSPI_HistoricalRecordsTest
{
    [TestClass]
    public class StatisticsDeviceTest
    {
        [DataTestMethod]
        [DataRow(StatisticsFunction.AverageStep)]
        [DataRow(StatisticsFunction.AverageLinear)]
        [DataRow(StatisticsFunction.MinValue)]
        [DataRow(StatisticsFunction.MaxValue)]
        public void AddDevice(StatisticsFunction function)
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock = TestHelper.SetupHsControllerAndSettings2(plugIn);

            int trackedRefId = 1039423;
            hsControllerMock.SetupFeature(trackedRefId, 1.132);

            hsControllerMock.SetupDevOrFeatureValue(trackedRefId, EProperty.Name, "A Unique Device");
            hsControllerMock.SetupDevOrFeatureValue(trackedRefId, EProperty.Location, "1 Loc");
            hsControllerMock.SetupDevOrFeatureValue(trackedRefId, EProperty.Location2, "2 Loc");
            hsControllerMock.SetupDevOrFeatureValue(trackedRefId, EProperty.AdditionalStatusData, new List<string> { "ad" });

            var collection = new StatusGraphicCollection();
            collection.Add(new StatusGraphic("path", new ValueRange(int.MinValue, int.MaxValue) { DecimalPlaces = 1 }));
            hsControllerMock.SetupDevOrFeatureValue(trackedRefId, EProperty.StatusGraphics, collection);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            string deviceName = "ssdfsd";
            JObject request = new()
            {
                { "name", new JValue(deviceName) },
                { "data", new JObject() {
                    { "TrackedRef", new JValue(trackedRefId) },
                    { "StatisticsFunction", new JValue(function) },
                    { "FunctionDurationSeconds", new JValue((long)new TimeSpan(1, 0, 10, 0).TotalSeconds) },
                    { "RefreshIntervalSeconds", new JValue((long)new TimeSpan(0, 0, 1, 30).TotalSeconds) } }
                },
            };

            //add
            string data2 = plugIn.Object.PostBackProc("devicecreate", request.ToString(), string.Empty, 0);

            // check no error is returned
            var result2 = JsonConvert.DeserializeObject<JObject>(data2);
            Assert.IsNotNull(result2);
            Assert.IsNull((string)result2["error"]);

            NewDeviceData newDataForDevice = hsControllerMock.CreatedDevices.First().Value;
            NewFeatureData newFeatureData = hsControllerMock.CreatedFeatures.First().Value;
            var trackedFeature = hsControllerMock.GetFeature(trackedRefId);

            // check proper device & feature was added
            Assert.IsNotNull(newDataForDevice);

            Assert.AreEqual(((string)newDataForDevice.Device[EProperty.Name]), deviceName);
            Assert.AreEqual(PlugInData.PlugInId, newDataForDevice.Device[EProperty.Interface]);
            Assert.AreEqual(trackedFeature.Location, newDataForDevice.Device[EProperty.Location]);
            Assert.AreEqual(trackedFeature.Location2, newDataForDevice.Device[EProperty.Location2]);

            Assert.AreEqual(PlugInData.PlugInId, newFeatureData.Feature[EProperty.Interface]);
            CollectionAssert.AreEqual(trackedFeature.AdditionalStatusData, (List<string>)newFeatureData.Feature[EProperty.AdditionalStatusData]);
            Assert.AreEqual(trackedFeature.Location, newFeatureData.Feature[EProperty.Location]);
            Assert.AreEqual(trackedFeature.Location2, newFeatureData.Feature[EProperty.Location2]);
#pragma warning disable S3265 // Non-flags enums should not be used in bitwise operations
            Assert.AreEqual((uint)(EMiscFlag.StatusOnly | EMiscFlag.SetDoesNotChangeLastChange | EMiscFlag.ShowValues),
                            newFeatureData.Feature[EProperty.Misc]);
#pragma warning restore S3265 // Non-flags enums should not be used in bitwise operations

            CollectionAssert.AreEqual((new HashSet<int> { hsControllerMock.CreatedDevices.First().Key }).ToImmutableArray(),
                                     ((HashSet<int>)newFeatureData.Feature[EProperty.AssociatedDevices]).ToImmutableArray());

            var plugExtraData = (PlugExtraData)newFeatureData.Feature[EProperty.PlugExtraData];

            Assert.AreEqual(1, plugExtraData.NamedKeys.Count);

            var data = JsonConvert.DeserializeObject<StatisticsDeviceData>(plugExtraData["data"]);

            Assert.AreEqual(trackedFeature.Ref, data.TrackedRef);

            switch (function)
            {
                case StatisticsFunction.AverageStep:
                    Assert.IsTrue(((string)newFeatureData.Feature[EProperty.Name]).StartsWith("Average(Step)"));
                    break;

                case StatisticsFunction.AverageLinear:
                    Assert.IsTrue(((string)newFeatureData.Feature[EProperty.Name]).StartsWith("Average(Linear)"));
                    break;

                case StatisticsFunction.MinValue:
                    Assert.IsTrue(((string)newFeatureData.Feature[EProperty.Name]).StartsWith("Minimum Value"));
                    break;

                case StatisticsFunction.MaxValue:
                    Assert.IsTrue(((string)newFeatureData.Feature[EProperty.Name]).StartsWith("Maximum Value"));
                    break;
            }

            Assert.AreEqual(function, data.StatisticsFunction);
            Assert.AreEqual((long)new TimeSpan(1, 0, 10, 0).TotalSeconds, data.FunctionDurationSeconds);
            Assert.AreEqual((long)new TimeSpan(0, 0, 1, 30).TotalSeconds, data.RefreshIntervalSeconds);

            var list1 = trackedFeature.StatusGraphics.Values;
            var list2 = ((StatusGraphicCollection)newFeatureData.Feature[EProperty.StatusGraphics]).Values;
            Assert.AreEqual(1, list1.Count);
            Assert.AreEqual(1, list2.Count);
            Assert.AreEqual(list1[0].Label, list2[0].Label);
            Assert.AreEqual(list1[0].IsRange, list2[0].IsRange);
            Assert.AreEqual(list1[0].ControlUse, list2[0].ControlUse);
            Assert.AreEqual(list1[0].HasAdditionalData, list2[0].HasAdditionalData);
            Assert.AreEqual(list1[0].TargetRange, list2[0].TargetRange);
        }

        [DataTestMethod]
        [DataRow("{\"name\":\"dev name\", \"data\": {\"StatisticsFunction\":3,\"FunctionDurationSeconds\":0,\"RefreshIntervalSeconds\":10}}", "Required property tracked ref not found in JSON")]
        [DataRow("", "Data is not correct")]
        public void AddDeviceErrorChecking(string format, string exception)
        {
            var plugIn = TestHelper.CreatePlugInMock();

            TestHelper.SetupHsControllerAndSettings2(plugIn);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            //add
            string data = plugIn.Object.PostBackProc("devicecreate", format, string.Empty, 0);
            Assert.IsNotNull(data);

            var jsonData = (JObject)JsonConvert.DeserializeObject(data);
            Assert.IsNotNull(jsonData);

            var errorMessage = jsonData["error"].Value<string>();
            Assert.IsFalse(string.IsNullOrWhiteSpace(errorMessage));
            StringAssert.Contains(errorMessage, exception);
        }

        [DataTestMethod]
        [DataRow(StatisticsFunction.AverageStep)]
        [DataRow(StatisticsFunction.AverageLinear)]
        [DataRow(StatisticsFunction.MinValue)]
        [DataRow(StatisticsFunction.MaxValue)]
        public void DeviceIsUpdated(StatisticsFunction statisticsFunction)
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock = TestHelper.SetupHsControllerAndSettings2(plugIn);

            DateTime aTime = new(2222, 2, 2, 2, 2, 2, DateTimeKind.Local);

            int statsDeviceRefId = 100;
            int statsFeatureRefId = 1000;
            int trackedDeviceRefId = 10;
            TestHelper.SetupStatisticsFeature(statisticsFunction, plugIn, hsControllerMock, aTime,
                                             statsDeviceRefId, statsFeatureRefId, trackedDeviceRefId);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            TestHelper.WaitForRecordCountAndDeleteAll(plugIn, trackedDeviceRefId, 1);

            TestHelper.RaiseHSEventAndWait(plugIn, hsControllerMock,
                                           Constants.HSEvent.VALUE_CHANGE,
                                           trackedDeviceRefId, 10, "10", aTime.AddMinutes(-10), 1);
            TestHelper.RaiseHSEventAndWait(plugIn, hsControllerMock,
                                           Constants.HSEvent.VALUE_CHANGE,
                                           trackedDeviceRefId, 20, "20", aTime.AddMinutes(-5), 2);

            Assert.IsTrue(plugIn.Object.UpdateStatisticsFeature(statsFeatureRefId));

            double ExpectedValue = 0;
            switch (statisticsFunction)
            {
                case StatisticsFunction.AverageStep:
                    ExpectedValue = ((10D * 5 * 60) + (20D * 5 * 60)) / 600D; break;
                case StatisticsFunction.AverageLinear:
                    ExpectedValue = ((15D * 5 * 60) + (20D * 5 * 60)) / 600D; break;
                case StatisticsFunction.MinValue:
                    ExpectedValue = 10D; break;
                case StatisticsFunction.MaxValue:
                    ExpectedValue = 20D; break;

                default:
                    Assert.Fail();
                    break;
            }

            TestHelper.WaitTillExpectedValue(hsControllerMock, statsFeatureRefId, ExpectedValue);

            Assert.AreEqual(false, hsControllerMock.GetFeatureValue(statsFeatureRefId, EProperty.InvalidValue));
        }

        [TestMethod]
        public void DeviceIsUpdatedRounded()
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock =
                TestHelper.SetupHsControllerAndSettings2(plugIn);

            DateTime aTime = new(2222, 2, 2, 2, 2, 2, DateTimeKind.Local);

            int statsDeviceRefId = 100;
            int statsFeatureRefId = 1000;
            int trackedDeviceRefId = 99;

            TestHelper.SetupStatisticsFeature(StatisticsFunction.AverageStep, plugIn, hsControllerMock, aTime,
                                  statsDeviceRefId, statsFeatureRefId, trackedDeviceRefId);

            List<StatusGraphic> statusGraphics = new() { new StatusGraphic("path", new ValueRange(int.MinValue, int.MaxValue) { DecimalPlaces = 1 }) };
            hsControllerMock.SetupDevOrFeatureValue(trackedDeviceRefId, EProperty.StatusGraphics, statusGraphics);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            TestHelper.WaitForRecordCountAndDeleteAll(plugIn, trackedDeviceRefId, 1);

            TestHelper.RaiseHSEventAndWait(plugIn, hsControllerMock,
                                           Constants.HSEvent.VALUE_CHANGE,
                                           trackedDeviceRefId, 11.85733, "11.2", aTime.AddMinutes(-10), 1);

            plugIn.Object.UpdateStatisticsFeature(statsDeviceRefId);

            TestHelper.WaitTillExpectedValue(hsControllerMock, statsFeatureRefId, 11.9D);

            Assert.AreEqual(false, hsControllerMock.GetFeatureValue(statsFeatureRefId, EProperty.InvalidValue));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void DevicePolled(bool device)
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock = TestHelper.SetupHsControllerAndSettings2(plugIn);

            DateTime aTime = new(2222, 2, 2, 2, 2, 2, DateTimeKind.Local);

            int statsDeviceRefId = 100;
            int statsFeatureRefId = 1000;
            int trackedDeviceRefId = 99;

            TestHelper.SetupStatisticsFeature(StatisticsFunction.AverageStep, plugIn, hsControllerMock, aTime,
                                  statsDeviceRefId, statsFeatureRefId, trackedDeviceRefId);

            List<StatusGraphic> statusGraphics = new() { new StatusGraphic("path", new ValueRange(int.MinValue, int.MaxValue) { DecimalPlaces = 1 }) };
            hsControllerMock.SetupDevOrFeatureValue(trackedDeviceRefId, EProperty.StatusGraphics, statusGraphics);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            TestHelper.WaitForRecordCountAndDeleteAll(plugIn, trackedDeviceRefId, 1);

            TestHelper.RaiseHSEventAndWait(plugIn, hsControllerMock,
                                           Constants.HSEvent.VALUE_CHANGE,
                                           trackedDeviceRefId, 11.85733, "11.2", aTime.AddMinutes(-10), 1);

            Assert.AreEqual(EPollResponse.Ok, plugIn.Object.UpdateStatusNow(device ? statsDeviceRefId : statsFeatureRefId));

            TestHelper.WaitTillExpectedValue(hsControllerMock, statsFeatureRefId, 11.9D);

            Assert.AreEqual(false, hsControllerMock.GetFeatureValue(statsFeatureRefId, EProperty.InvalidValue));
        }

        [DataTestMethod]
        [DataRow(StatisticsFunction.AverageStep)]
        [DataRow(StatisticsFunction.AverageLinear)]
        public void EditDevice(StatisticsFunction function)
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock = TestHelper.SetupHsControllerAndSettings2(plugIn);

            DateTime aTime = new(2222, 2, 2, 2, 2, 2, DateTimeKind.Local);

            int statsDeviceRefId = 100;
            int statsFeatureRefId = 1000;
            int trackedDeviceRefId = 10;

            TestHelper.SetupStatisticsFeature(StatisticsFunction.AverageLinear, plugIn, hsControllerMock, aTime,
                                  statsDeviceRefId, statsFeatureRefId, trackedDeviceRefId);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            JObject editRequest = new()
            {
                { "ref" , new JValue(statsFeatureRefId) },
                { "data" , new JObject() {
                    { "TrackedRef", new JValue(trackedDeviceRefId) },
                    { "StatisticsFunction", new JValue(function) },
                    { "FunctionDurationSeconds", new JValue((long)new TimeSpan(5, 1, 10, 3).TotalSeconds) },
                    { "RefreshIntervalSeconds", new JValue((long)new TimeSpan(0, 5, 1, 30).TotalSeconds) },
                }}
            };

            // edit
            string data2 = plugIn.Object.PostBackProc("deviceedit", editRequest.ToString(), string.Empty, 0);

            // no error is returned
            var result2 = JsonConvert.DeserializeObject<JObject>(data2);
            Assert.IsNotNull(result2);
            Assert.IsNull((string)result2["error"]);

            // get return function value for feature
            string json = plugIn.Object.GetStatisticDeviceDataAsJson(statsFeatureRefId);
            Assert.AreEqual(JsonConvert.DeserializeObject<StatisticsDeviceData>(editRequest["data"].ToString()),
                            JsonConvert.DeserializeObject<StatisticsDeviceData>(json));

            var plugExtraData = (PlugExtraData)hsControllerMock.GetFeatureValue(statsFeatureRefId, EProperty.PlugExtraData);
            Assert.AreEqual(1, plugExtraData.NamedKeys.Count);
            Assert.AreEqual(JsonConvert.DeserializeObject<StatisticsDeviceData>(plugExtraData["data"]),
                            JsonConvert.DeserializeObject<StatisticsDeviceData>(json));
        }

        [TestMethod]
        public void EditDeviceFailsForInvalidDevice()
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock =
                TestHelper.SetupHsControllerAndSettings2(plugIn);

            DateTime aTime = new(2222, 2, 2, 2, 2, 2, DateTimeKind.Local);

            int statsDeviceRefId = 199;
            int statsFeatureRefId = 1000;
            int trackedDeviceRefId = 100;

            TestHelper.SetupStatisticsFeature(StatisticsFunction.AverageLinear, plugIn, hsControllerMock, aTime,
                                  statsDeviceRefId, statsFeatureRefId, trackedDeviceRefId);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            JObject editRequest = new()
            {
                { "ref" , new JValue(trackedDeviceRefId) }, // wrong ref
                { "data" , new JObject() {
                    { "TrackedRef", new JValue(statsFeatureRefId) },
                    { "StatisticsFunction", new JValue(StatisticsFunction.AverageLinear) },
                    { "FunctionDurationSeconds", new JValue((long)new TimeSpan(5, 1, 10, 3).TotalSeconds) },
                    { "RefreshIntervalSeconds", new JValue((long)new TimeSpan(0, 5, 1, 30).TotalSeconds) },
                }}
            };

            // edit
            string data2 = plugIn.Object.PostBackProc("deviceedit", editRequest.ToString(), string.Empty, 0);

            // error is returned
            var result2 = JsonConvert.DeserializeObject<JObject>(data2);
            Assert.IsNotNull(result2);
            StringAssert.Contains((string)result2["error"], $"Device or feature {trackedDeviceRefId} not a plugin feature");
        }

        [TestMethod]
        public void StatisticsDeviceIsDeleted()
        {
            var plugIn = TestHelper.CreatePlugInMock();
            var hsControllerMock =
                TestHelper.SetupHsControllerAndSettings2(plugIn);

            DateTime aTime = new(2222, 2, 2, 2, 2, 2, DateTimeKind.Local);

            int statsDeviceRefId = 1080;
            int statsFeatureRefId = 1000;
            int trackedDeviceRefId = 100;

            TestHelper.SetupStatisticsFeature(StatisticsFunction.AverageStep, plugIn, hsControllerMock, aTime,
                                  statsDeviceRefId, statsFeatureRefId, trackedDeviceRefId);

            using PlugInLifeCycle plugInLifeCycle = new(plugIn);

            Assert.IsTrue(TestHelper.TimedWaitTillTrue(() => plugIn.Object.UpdateStatisticsFeature(statsDeviceRefId)));

            Assert.IsTrue(hsControllerMock.RemoveFeatureOrDevice(statsDeviceRefId));
            Assert.IsTrue(hsControllerMock.RemoveFeatureOrDevice(statsFeatureRefId));
            plugIn.Object.HsEvent(Constants.HSEvent.CONFIG_CHANGE,
                                  new object[] { null, null, null, statsDeviceRefId, 2 });

            Assert.IsTrue(TestHelper.TimedWaitTillTrue(() => !plugIn.Object.UpdateStatisticsFeature(statsDeviceRefId)));

            // not more tracking after delete
            Assert.IsFalse(plugIn.Object.UpdateStatisticsFeature(statsDeviceRefId));
            Assert.IsFalse(plugIn.Object.UpdateStatisticsFeature(statsFeatureRefId));
        }
    }
}