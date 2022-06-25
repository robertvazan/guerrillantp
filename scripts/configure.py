# This script generates and updates project configuration files.

import pathlib

# We are assuming that project-config is available in sibling directory.
# Checkout from https://github.com/robertvazan/project-config
project_directory = lambda: pathlib.Path(__file__).parent.parent
config_directory = lambda: project_directory().parent/'project-config'

exec((config_directory()/'src'/'net.py').read_text())

root_namespace = lambda: 'GuerrillaNtp'
inception_year = lambda: 2014
nuget_description = lambda: 'Simple NTP (SNTP) client that can be embedded in desktop applications to provide accurate network time even when the system clock is unsynchronized.'
nuget_tags = lambda: 'NTP; SNTP; time; clock; network'
extra_sln_projects = lambda: ['GuerrillaNtp.Cli']

generate()

