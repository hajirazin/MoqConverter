using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MoqConverter.Core.Projects
{
    public class Solution
    {
        public Solution(string targetDirectory)
        {
            var projectFiles = Directory.GetFiles(targetDirectory, "*.csproj", SearchOption.AllDirectories);
            Projects = projectFiles.Select(f => new Project(f)).ToList();
        }

        public List<Project> Projects { get; }
    }
}
