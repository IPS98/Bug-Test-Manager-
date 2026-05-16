using BugTestManager.Application.Requests;
using BugTestManager.Application.Results;

namespace BugTestManager.Application.Abstractions;

public interface ITestSuiteManagementService
{
    CreateTestSuiteResult CreateTestSuite(CreateTestSuiteRequest request);

    Guid CreateRevision(CreateTestSuiteRevisionRequest request);

    Guid CreateSection(CreateTemplateSectionRequest request);

    Guid CreateTestCase(CreateTestCaseTemplateRequest request);

    Guid CreateTestStep(CreateTestStepTemplateRequest request);

    void UpdateTestSuite(UpdateTestSuiteRequest request);

    void UpdateRevision(UpdateTestSuiteRevisionRequest request);

    void UpdateSection(UpdateTemplateSectionRequest request);

    void UpdateTestCase(UpdateTestCaseTemplateRequest request);

    void UpdateTestStep(UpdateTestStepTemplateRequest request);

    void DeleteTestSuite(Guid testSuiteId);

    void DeleteRevision(Guid testSuiteRevisionId);

    void DeleteSection(Guid sectionId);

    void DeleteTestCase(Guid testCaseId);

    void DeleteTestStep(Guid testStepId);
}
