﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    public class SchemaCompareServiceTests
    {
        private const string SourceScript = @"CREATE TABLE [dbo].[table1]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [Date] DATE NOT NULL
)
CREATE TABLE [dbo].[table2]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] NCHAR(10) NULL
)";

        private const string TargetScript = @"CREATE TABLE [dbo].[table2]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] NCHAR(10) NULL,
    [col2] NCHAR(10) NULL
)
CREATE TABLE [dbo].[table3]
(
    [ID] INT NOT NULL PRIMARY KEY,
    [col1] INT NULL,
)";

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDacpacToDacpac()
        {

            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            // create dacpacs from databases
            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            try
            {
                string sourceDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(sourceDb);
                string targetDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(targetDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpacFilePath;
                targetInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                targetInfo.PackageFilePath = targetDacpacFilePath;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, null, null);
                ValidateSchemaCompareWithExcludeIncludeResults(schemaCompareOperation);

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
                SchemaCompareTestUtils.VerifyAndCleanup(targetDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }

            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDatabaseToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                ValidateSchemaCompareWithExcludeIncludeResults(schemaCompareOperation);
            }
            finally
            {
                // cleanup
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareRequestDatabaseToDacpac()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                string targetDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(targetDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                targetInfo.PackageFilePath = targetDacpacFilePath;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, null);
                ValidateSchemaCompareWithExcludeIncludeResults(schemaCompareOperation);

                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(targetDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareGenerateScriptRequestDatabaseToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                
                // generate script params
                var generateScriptParams = new SchemaCompareGenerateScriptParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                ValidateSchemaCompareScriptGenerationWithExcludeIncludeResults(schemaCompareOperation, generateScriptParams);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, TargetScript, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                string sourceDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(sourceDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpacFilePath;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);

                // generate script
                var generateScriptParams = new SchemaCompareGenerateScriptParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                ValidateSchemaCompareScriptGenerationWithExcludeIncludeResults(schemaCompareOperation, generateScriptParams);
                
                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaComparePublishChangesRequestDacpacToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "SchemaCompareTarget");
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SchemaCompareTest");
            Directory.CreateDirectory(folderPath);

            try
            {
                string sourceDacpacFilePath = SchemaCompareTestUtils.CreateDacpac(sourceDb);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Dacpac;
                sourceInfo.PackageFilePath = sourceDacpacFilePath;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);
                var enumerator = schemaCompareOperation.ComparisonResult.Differences.GetEnumerator();
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table1]"));
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table2]"));

                // update target
                var publishChangesParams = new SchemaComparePublishChangesParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                SchemaComparePublishChangesOperation publishChangesOperation = new SchemaComparePublishChangesOperation(publishChangesParams, schemaCompareOperation.ComparisonResult);
                publishChangesOperation.Execute(TaskExecutionMode.Execute);
                Assert.True(publishChangesOperation.PublishResult.Success);
                Assert.Empty(publishChangesOperation.PublishResult.Errors);

                // Verify that there are no differences after the publish by running the comparison again
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.True(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.Empty(schemaCompareOperation.ComparisonResult.Differences);
                
                // cleanup
                SchemaCompareTestUtils.VerifyAndCleanup(sourceDacpacFilePath);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }

        private async Task<Mock<RequestContext<SchemaCompareResult>>> SendAndValidateSchemaComparePublishChangesRequestDatabaseToDatabase()
        {
            var result = SchemaCompareTestUtils.GetLiveAutoCompleteTestObjects();
            var schemaCompareRequestContext = new Mock<RequestContext<SchemaCompareResult>>();
            schemaCompareRequestContext.Setup(x => x.SendResult(It.IsAny<SchemaCompareResult>())).Returns(Task.FromResult(new object()));

            SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, SourceScript, "SchemaCompareSource");
            SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, null, "SchemaCompareTarget");

            try
            {
                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo();
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo();

                sourceInfo.EndpointType = SchemaCompareEndpointType.Database;
                sourceInfo.DatabaseName = sourceDb.DatabaseName;
                targetInfo.EndpointType = SchemaCompareEndpointType.Database;
                targetInfo.DatabaseName = targetDb.DatabaseName;

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation schemaCompareOperation = new SchemaCompareOperation(schemaCompareParams, result.ConnectionInfo, result.ConnectionInfo);
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);
                var enumerator = schemaCompareOperation.ComparisonResult.Differences.GetEnumerator();
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table1]"));
                enumerator.MoveNext();
                Assert.True(enumerator.Current.SourceObject.Name.ToString().Equals("[dbo].[table2]"));

                // update target
                var publishChangesParams = new SchemaComparePublishChangesParams
                {
                    TargetDatabaseName = targetDb.DatabaseName,
                    OperationId = schemaCompareOperation.OperationId,
                };

                SchemaComparePublishChangesOperation publishChangesOperation = new SchemaComparePublishChangesOperation(publishChangesParams, schemaCompareOperation.ComparisonResult);
                publishChangesOperation.Execute(TaskExecutionMode.Execute);
                Assert.True(publishChangesOperation.PublishResult.Success);
                Assert.Empty(publishChangesOperation.PublishResult.Errors);

                // Verify that there are no differences after the publish by running the comparison again
                schemaCompareOperation.Execute(TaskExecutionMode.Execute);

                Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
                Assert.True(schemaCompareOperation.ComparisonResult.IsEqual);
                Assert.Empty(schemaCompareOperation.ComparisonResult.Differences);
            }
            finally
            {
                sourceDb.Cleanup();
                targetDb.Cleanup();
            }
            return schemaCompareRequestContext;
        }
        
        private void ValidateSchemaCompareWithExcludeIncludeResults(SchemaCompareOperation schemaCompareOperation)
        {
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            // create Diff Entry from Difference
            
            DiffEntry diff = SchemaCompareOperation.CreateDiffEntry(schemaCompareOperation.ComparisonResult.Differences.First(), null);

            int initial = schemaCompareOperation.ComparisonResult.Differences.Count();
            SchemaCompareNodeParams schemaCompareExcludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = false,
                TaskExecutionMode = TaskExecutionMode.Execute
            };
            SchemaCompareIncludeExcludeNodeOperation nodeExcludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareExcludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeExcludeOperation.Execute(TaskExecutionMode.Execute);

            int afterExclude = schemaCompareOperation.ComparisonResult.Differences.Count();

            Assert.True(initial == afterExclude, $"Changes should be same again after excluding/including, before {initial}, now {afterExclude}");
            
            SchemaCompareNodeParams schemaCompareincludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = true,
                TaskExecutionMode = TaskExecutionMode.Execute
            };

            SchemaCompareIncludeExcludeNodeOperation nodeIncludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareincludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeIncludeOperation.Execute(TaskExecutionMode.Execute);
            int afterInclude = schemaCompareOperation.ComparisonResult.Differences.Count();


            Assert.True(initial == afterInclude, $"Changes should be same again after excluding/including, before:{initial}, now {afterInclude}");
        }
        
        private void ValidateSchemaCompareScriptGenerationWithExcludeIncludeResults(SchemaCompareOperation schemaCompareOperation, SchemaCompareGenerateScriptParams generateScriptParams)
        {
            schemaCompareOperation.Execute(TaskExecutionMode.Execute);

            Assert.True(schemaCompareOperation.ComparisonResult.IsValid);
            Assert.False(schemaCompareOperation.ComparisonResult.IsEqual);
            Assert.NotNull(schemaCompareOperation.ComparisonResult.Differences);

            SchemaCompareGenerateScriptOperation generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute();

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string initialScript = generateScriptOperation.ScriptGenerationResult.Script;

            // create Diff Entry from on Difference
            DiffEntry diff = SchemaCompareOperation.CreateDiffEntry(schemaCompareOperation.ComparisonResult.Differences.First(), null);

            int initial = schemaCompareOperation.ComparisonResult.Differences.Count();
            SchemaCompareNodeParams schemaCompareExcludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = false,
                TaskExecutionMode = TaskExecutionMode.Execute
            };
            SchemaCompareIncludeExcludeNodeOperation nodeExcludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareExcludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeExcludeOperation.Execute(TaskExecutionMode.Execute);

            int afterExclude = schemaCompareOperation.ComparisonResult.Differences.Count();

            Assert.True(initial == afterExclude, $"Changes should be same again after excluding/including, before {initial}, now {afterExclude}");

            generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute();

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string afterExcludeScript = generateScriptOperation.ScriptGenerationResult.Script;
            Assert.True(initialScript.Length > afterExcludeScript.Length, $"Script should be affected (less statements) exclude operation, before {initialScript}, now {afterExcludeScript}");

            SchemaCompareNodeParams schemaCompareincludeNodeParams = new SchemaCompareNodeParams()
            {
                OperationId = schemaCompareOperation.OperationId,
                DiffEntry = diff,
                IncludeRequest = true,
                TaskExecutionMode = TaskExecutionMode.Execute
            };

            SchemaCompareIncludeExcludeNodeOperation nodeIncludeOperation = new SchemaCompareIncludeExcludeNodeOperation(schemaCompareincludeNodeParams, schemaCompareOperation.ComparisonResult);
            nodeIncludeOperation.Execute(TaskExecutionMode.Execute);
            int afterInclude = schemaCompareOperation.ComparisonResult.Differences.Count();
            
            Assert.True(initial == afterInclude, $"Changes should be same again after excluding/including:{initial}, now {afterInclude}");

            generateScriptOperation = new SchemaCompareGenerateScriptOperation(generateScriptParams, schemaCompareOperation.ComparisonResult);
            generateScriptOperation.Execute();

            Assert.True(generateScriptOperation.ScriptGenerationResult.Success);
            string afterIncludeScript = generateScriptOperation.ScriptGenerationResult.Script;
            Assert.True(initialScript.Length == afterIncludeScript.Length, $"Changes should be same as inital since we included what we excluded, before {initialScript}, now {afterIncludeScript}");
        }
        
        /// <summary>
        /// Verify the schema compare request comparing two dacpacs
        /// </summary>
        [Fact]
        public async void SchemaCompareDacpacToDacpac()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDacpacToDacpac());
        }

        /// <summary>
        /// Verify the schema compare request comparing a two databases
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDatabase()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDatabaseToDatabase());
        }

        /// <summary>
        /// Verify the schema compare request comparing a database to a dacpac
        /// </summary>
        [Fact]
        public async void SchemaCompareDatabaseToDacpac()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareRequestDatabaseToDacpac());
        }

        /// <summary>
        /// Verify the schema compare generate script request comparing a database to a database
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDatabaseToDatabase()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareGenerateScriptRequestDatabaseToDatabase());
        }

        /// <summary>
        /// Verify the schema compare generate script request comparing a dacpac to a database
        /// </summary>
        [Fact]
        public async void SchemaCompareGenerateScriptDacpacToDatabase()
        {
            Assert.NotNull(await SendAndValidateSchemaCompareGenerateScriptRequestDacpacToDatabase());
        }

        /// <summary>
        /// Verify the schema compare publish changes request comparing a dacpac to a database
        /// </summary>
        [Fact]
        public async void SchemaComparePublishChangesDacpacToDatabase()
        {
            Assert.NotNull(await SendAndValidateSchemaComparePublishChangesRequestDacpacToDatabase());
        }

        /// <summary>
        /// Verify the schema compare publish changes request comparing a database to a database
        /// </summary>
        [Fact]
        public async void SchemaComparePublishChangesDatabaseToDatabase()
        {
            Assert.NotNull(await SendAndValidateSchemaComparePublishChangesRequestDatabaseToDatabase());
        }
    }
}
