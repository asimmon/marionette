#!/usr/bin/env bash
# Lists analyzer warnings of a project, without the public API tracking noise.
export PATH=/root/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build "${1:-src/Askaiser.Marionette}" -v q --no-incremental 2>&1 | grep -E "warning|error" | grep -v "RS0016\|RS0017" | sed -E 's#^/home/user/marionette/##; s# \[/home.*##' | sort -u
