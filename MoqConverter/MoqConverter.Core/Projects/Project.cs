using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MoqConverter.Core.Projects
{
    public class Project
    {
        private readonly ProjectXml _projectXml;

        public Project(string projectFile)
        {
            _projectXml = new ProjectXml(projectFile);
            ProjectPath = projectFile;
        }

        public bool HasRhinoMockReference => _projectXml.HasRhinoMockReference;
        public bool HasMoqReference => _projectXml.HasMoqReference;
        public bool HasNSubstituteReference => _projectXml.HasNSubstituteReference;
        
        public string ProjectPath { get; }

        public void RemoveRhinoMockReference() => _projectXml.RemoveRhinoMockReference();

        public void AddMoqReference() => _projectXml.AddMoqReference();

        public void RemoveNSubstituteReference() => _projectXml.RemoveNSubstituteReference();

        public List<string> Files
        {
            get
            {
                var directory = Path.GetDirectoryName(ProjectPath);
                if (directory == null) return new List<string>();
                var filePaths = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                return filePaths.Where(f => !f.Contains("bin") && !f.Contains("obj") && !f.Contains("AssemblyInfo")).ToList();
            }
        }
    }
}
