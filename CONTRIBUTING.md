# Contributing to DBA Dash

You are considering contributing to DBA Dash - That's great, we need your help!  You can contribute to DBA Dash in a number of ways:

- Fix bugs and implement new features
- Improve the documentation
- Answer questions in the [discussions](https://github.com/trimble-oss/dba-dash/discussions)
- [Report a bug](#what-makes-a-good-issue) (Issue)
- [Suggest a new feature](#what-makes-a-good-issue) (Issue)
- Help spread the word.  e.g. Blog posts, presentations & video.  Or just talking to people about the project.

This page will focus on contributing code/documentation to the repository.  

## Contributing Steps

Are you a Trimble employee?  Please review the [contribution section](https://trimble-oss.github.io/contribute) including the [contribution guildelines](https://trimble-oss.github.io/contribute/guidelines/).  You don't need to be a Trimble employee to contribute and we welcome external contributors.  The steps to contribute are listed below:

- [Create a new issue](https://github.com/trimble-oss/dba-dash/issues) or comment on an existing issue saying you would like to work on it
- Wait for feedback

*It's good to get some feedback on your issue before you start coding.  This avoids any wasted effort where two people are working on the same problem.  Also, some fixes might not be a good fit for this project and it's good to get that feedback upfront before you invest a large amount of time coding a new feature.  For simple typo fixes to the documentation this is less important.*

- Fork the project
- Clone your fork of the project
- Create a branch for your change
- Commit your changes to the branch
- Push your changes
- Create a pull request

You might make several commits in your branch before you are ready to submit a pull request.  If appropriate, squash your commits to create a clean history before you submit a pull request.

If you need to update your branch, you should use **rebase** instead of merge.  This is to ensure a clean, concise and consistent commit history, following the Trimble [contributor guidelines](https://trimble-oss.github.io/contribute/guidelines/).

This document only provides some high level steps and assumes some knowledge of Git/GitHub.  The [contribution guildelines](https://trimble-oss.github.io/contribute/guidelines/) provide some more detail, but it's intended for consumption by Trimble employees.  If you are an external user the document is still be worth a read, but ignore the top section on access configuration. 

## What makes a good issue

It's important to provide a clear explanation of the bug or the feature you would like to be implemented.  For bugs, please include error messages, the version of DBA Dash been used and any details about your environment that might be relevant. Is the bug intermittent?  What steps are needed to reproduce the issue? What version of SQL Server are you using?  Including screenshots can help a lot - It's easy to paste them into GitHub issues.

For new features, provide a good explanation of the new feature and why it's important.  It might be useful to include drawings or some sort of visual aid in some cases.

*Note: Please take care not to include any sensitive data in the issue.  Issues are public for anyone to read.*

DBA Dash is an open source project and relies on code contributions from the community.  If you are willing to implement the changes for the issue yourself, please include this on the issue. It's OK to submit issues if you are not able to provide the code changes yourself.

[Create an issue](https://github.com/trimble-oss/dba-dash/issues)

## Will my Pull Request be Accepted?

This project is looking for code contributions from the community, but occasionally it might be necessary to reject a pull request.  There are a number of reasons why this might occur.  For example:

- Does it break something?  
- Does it work with all versions of SQL Server supported by DBA Dash? 

*Note: It's OK to use features supported by newer versions of SQL Server but it needs to be written in a way that doesn't fail with older versions.*

- Does it add significant overhead? 
- Is it a very niche feature? 
- What other tradeoffs need to be considered? 
- Code quality and maintenance considerations? 
- Are there conflicts that need to be resolved?  

If your pull request is rejected, feedback will be provided.  It might just be a case of making a few minor alterations to get your pull request accepted.

We want code contributions and hopefully your code changes will make it into the repository.  Making the effort to submit a pull request will always be appreciated even if it is rejected for some reason. As a last resort there is always an option to keep the changes in your own fork of the project.

## I don't know where to start

It can be challenging to get started contributing to a new project.  There are some [developer notes here](/Docs/developer.md) that might be useful.  It's OK to ask questions.  You don't need to be an expert in C# to contribute - if you are more comfortable writing SQL code, there are plenty of areas of the project you can contribute to.  Contributions to the documentation are also very welcome.  

