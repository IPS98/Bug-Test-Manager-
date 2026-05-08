using BugTestManager.Application.Requests;
using BugTestManager.Application.Results;

namespace BugTestManager.Application.Abstractions;

public interface ITestSuiteManagementService
{
    CreateTestSuiteResult CreateTestSuite(CreateTestSuiteRequest request);

    Guid CreateSection(CreateTemplateSectionRequest request);
}
