# Contributing to MonkeyBot

First off, thank you for considering contributing to MonkeyBot to make it even better than it is. Here are the guidelines we'd like you to follow:

 - [Question or Problem?](#question)
 - [Issues and Bugs](#issue)
 - [Feature Requests](#feature)
 - [Submission Guidelines](#submit)

## <a name="question"></a> Got a Question or Problem?

If you have questions about how to use MonkeyBot, please join our [Discord](https://discord.gg/u43XvME).

## <a name="issue"></a> Found an Issue?

If you find a bug in the source code or a mistake in the documentation, you can help us by
submitting an issue to our [GitHub Repository](https://github.com/MarkusKgit/MonkeyBot). Even better you can submit a Pull Request
with a fix.

**Please see the [Submission Guidelines](#submit) below.**

## <a name="feature"></a> Want a Feature?

You can request a new feature by submitting an issue to our [GitHub Repository](https://github.com/MarkusKgit/MonkeyBot).  If you
would like to implement a new feature then consider what kind of change it is:

* **Major Changes** that you wish to contribute to the project should be discussed first in [Discord](https://discord.gg/u43XvME)
* **Small Changes** can be crafted and submitted to the [GitHub Repository](https://github.com/MarkusKgit/MonkeyBot) as a Pull Request against the development branch.

## <a name="submit"></a> Submission Guidelines

### Submitting an Issue

If your issue appears to be a bug, and hasn't been reported, open a new issue.

Providing the following information will increase the chances of your issue being dealt with
quickly:

* **Overview of the Issue** - if an error is being thrown a stack trace helps
* **Motivation for or Use Case** - explain why this is a bug for you
* **Operating System** - is this a problem with all OS or only specific ones?
* **Reproduce the Error** - provide a example or an unambiguous set of steps.
* **Related Issues** - has a similar issue been reported before?
* **Suggest a Fix** - if you can't fix the bug yourself, perhaps you can point to what might be
  causing the problem (line of code or commit)

### Submitting a Pull Request
Before you submit your pull request consider the following guidelines:

* Fork the repository
* Make your changes in a new git branch:

    ```shell
    git checkout -b my-fix-branch develop
    ```

* Create your patch following the repositories code style.
* Commit your changes using a descriptive commit message.

    ```shell
    git commit -a
    ```
  Note: the optional commit `-a` command line option will automatically "add" and "rm" edited files.

* Build your changes locally to ensure everything compiles

* Push your branch to GitHub:

    ```shell
    git push origin my-fix-branch
    ```

* Open a Pull Request against the development branch:

That's it! Thank you for your contribution!

#### After your pull request is merged

After your pull request is merged, you can safely delete your branch and pull the changes
from the main (upstream) repository:

* Delete the remote branch on GitHub either through the GitHub web UI or your local shell as follows:

    ```shell
    git push origin --delete my-fix-branch
    ```

* Check out the development branch:

    ```shell
    git checkout develop -f
    ```

* Delete the local branch:

    ```shell
    git branch -D my-fix-branch
    ```

* Update your development branch with the latest upstream version:

    ```shell
    git pull --ff upstream develop
    ```
