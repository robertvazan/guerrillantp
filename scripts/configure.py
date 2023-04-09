# This script generates and updates project configuration files.

# Run this script with rvscaffold in PYTHONPATH
import rvscaffold as scaffold

class Project(scaffold.Net):
    def script_path_text(self): return __file__
    def root_namespace(self): return 'GuerrillaNtp'
    def inception_year(self): return 2014
    def nuget_description(self): return 'Simple NTP (SNTP) client that can be embedded in desktop applications to provide accurate network time even when the system clock is unsynchronized.'
    def nuget_tags(self): return 'NTP; SNTP; time; clock; network'
    def project_status(self): return self.stable_status()
    def backport_frameworks(self): return ['2.0']

    def dependencies(self):
        yield from super().dependencies()
        yield self.use('System.Memory:4.5.5')

    def documentation_links(self):
        yield from super().documentation_links()
        yield 'CLI demo', 'https://guerrillantp.machinezoo.com/cli'

    def notice_text(self):
        return '''\
            This code contains imported, though modified, code from
            https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Index.cs
            https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Range.cs
            which is MIT licensed: https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/LICENSE.TXT
        '''

Project().generate()
