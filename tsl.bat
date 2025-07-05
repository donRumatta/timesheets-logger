@echo off

SET EXE_PATH="timesheets-logger\src\TimesheetsLogger\TimesheetsLogger\bin\Debug\net9.0"
SET FILE_PATH="Desktop\WL.txt"
SET JIRA_URL="https://jira.com/rest/api/2/"
SET JIRA_LOGIN="Moon"
SET JIRA_PASSWORD="Dalaran"

%EXE_PATH%\tsl.exe lw -fp %FILE_PATH% -ju %JIRA_URL% -jl %JIRA_LOGIN% -jp %JIRA_PASSWORD%
pause