# Data Model

This document describes the planned data model direction.

The model should support reusable templates, user-defined test suites, optional revisions, manual test sessions, dynamic fields, tags, links, filtering, bug tracking, attachments, and audit history.

## Core Entities

### Product

Represents the tested product or application.

Possible fields:

- Id
- Name
- Description
- CreatedAt
- UpdatedAt

### ProductVersion

Represents a specific tested version.

Possible fields:

- Id
- ProductId
- VersionName
- ReleaseDate
- Notes

### TestSuite

Represents a user-defined group of tests. It can be a standard, application window, feature group, product area, or any other test base.

Possible fields:

- Id
- ProductId
- Name
- Description
- IsActive
- RevisionIsRequired

### TestSuiteRevision

Represents a revision of a test suite, for example Revision A or Revision B. Not every test suite requires revisions.

Possible fields:

- Id
- TestSuiteId
- RevisionName
- Description
- EffectiveDate
- IsActive

### TestTemplate

Reusable test structure. A template belongs to a test suite and may optionally belong to a test suite revision.

Possible fields:

- Id
- TestSuiteId
- TestSuiteRevisionId, optional
- Name
- Description
- IsActive
- CreatedBy
- CreatedAt
- UpdatedAt

### TemplateRevision

Represents a version of the template itself.

Possible fields:

- Id
- TestTemplateId
- RevisionName
- Description
- CreatedBy
- CreatedAt
- IsActive

### TemplateSection

Represents a test suite section, application window, tab, sub-tab, or feature group in the template.

Possible fields:

- Id
- TemplateRevisionId
- ParentSectionId
- Name
- Category
- SortOrder

### TestCaseTemplate

Reusable test case inside a template section.

Possible fields:

- Id
- TemplateSectionId
- Title
- Description
- ExpectedResult
- SortOrder

### TestStepTemplate

Reusable step inside a test case.

Possible fields:

- Id
- TestCaseTemplateId
- StepText
- ExpectedResult
- SortOrder

### TestSession

A manual QA session/report created from a template for a specific product version or build. This does not mean the application runs automated tests.

Possible fields:

- Id
- ProductVersionId
- SourceTemplateId
- SourceTemplateRevisionId
- TestSuiteId
- TestSuiteRevisionId
- Name
- Status
- BuildNumber
- StartedAt
- CompletedAt
- CreatedBy
- CreatedAt

### TestSectionResult

Snapshot of a template section inside a manual test session.

Possible fields:

- Id
- TestSessionId
- ParentSectionResultId
- Name
- Category
- SortOrder
- Status

### TestCaseResult

Result of a test case inside a manual test session.

Possible fields:

- Id
- TestSessionId
- TestSectionResultId
- Title
- Status
- ActualResult
- Comment
- AssignedTo
- UpdatedBy
- UpdatedAt

### TestStepResult

Result of a test step.

Possible fields:

- Id
- TestCaseResultId
- StepText
- ExpectedResult
- ActualResult
- Status
- Comment
- TestedAt
- UpdatedBy
- UpdatedAt

### BugReport

Tracked issue found during testing.

Possible fields:

- Id
- Title
- Description
- Status
- Severity
- Priority
- AssignedTo
- FoundBy
- FoundAt
- FixedInVersion
- RelatedTestSessionId
- RelatedTestCaseResultId
- RelatedTestStepResultId

### BugComment

Comment or technical note on a bug.

Possible fields:

- Id
- BugReportId
- CommentText
- CreatedBy
- CreatedAt

### Attachment

File connected to a test, bug, comment, or report.

Possible fields:

- Id
- EntityType
- EntityId
- OriginalFileName
- StoredFileName
- RelativePath
- ContentType
- SizeBytes
- Checksum
- UploadedBy
- UploadedAt

### Tag

Represents a searchable label.

Possible fields:

- Id
- Name
- Color

### EntityTag

Connects a tag to a test, bug, template, section, or revision.

Possible fields:

- Id
- TagId
- EntityType
- EntityId

### EntityLink

Connects related items, for example a bug to a test case, a session to a previous session, or a report to a source template.

Possible fields:

- Id
- SourceEntityType
- SourceEntityId
- TargetEntityType
- TargetEntityId
- LinkType
- CreatedBy
- CreatedAt

### AuditLog

History record for important actions.

Possible fields:

- Id
- EntityType
- EntityId
- Action
- UserName
- UserRole
- ChangedAt
- OldValuesJson
- NewValuesJson

## Dynamic Fields

### CustomFieldDefinition

Defines a flexible field.

Possible fields:

- Id
- TargetEntityType
- Name
- FieldType
- IsRequired
- SortOrder
- OptionsJson
- IsActive

Each team can decide which fields are required. For example, firmware can be required for one template and optional for another.

### CustomFieldValue

Stores the value of a flexible field for one record.

Possible fields:

- Id
- FieldDefinitionId
- EntityType
- EntityId
- ValueJson

## Recommended Statuses

Test result statuses:

- Not Tested
- Pass
- Fail
- Blocked
- Not Applicable

Test session statuses:

- Draft
- In Progress
- Completed
- Approved
- Archived

Bug statuses:

- Open
- In Progress
- Fixed
- Ready For Retest
- Reopened
- Closed
- Rejected

Severity:

- Low
- Medium
- High
- Critical

Priority:

- Low
- Medium
- High
- Urgent
