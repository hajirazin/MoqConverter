using System;
using System.IO;
using System.Threading.Tasks;
using MoqConverter.Core.Projects;
using MoqConverter.Core.RhinoMockToMoq;

namespace MoqConverter.Core.Converters
{
    public interface IProjectConverter
    {
        void Convert(Project project);
    }

    public class ProjectConverter : IProjectConverter
    {
        public void Convert(Project project)
        {
            //if (!project.HasNSubstituteReference && !project.HasRhinoMockReference)
            //    return;

            Logger.Log($"Starting Convertion of project {Path.GetFileNameWithoutExtension(project.ProjectPath)}", ConsoleColor.Green);

            //if (project.HasRhinoMockReference)
            //    project.RemoveRhinoMockReference();

            //if (project.HasNSubstituteReference)
            //    project.RemoveNSubstituteReference();

            //if (!project.HasMoqReference)
            //    project.AddMoqReference();

            Parallel.ForEach(project.Files, projectFile =>
            {
                FileRewritter rewriter;
                //if (project.HasMoqReference)
                //{
                //    rewriter = new MoqVisitor();
                //    var fileConverter = new FileConverter(rewriter);
                //    fileConverter.Convert(projectFile);
                //}

               // if (project.HasRhinoMockReference)
                {
                    rewriter = new Rewritter();
                    var fileConverter = new FileConverter(rewriter);
                    fileConverter.Convert(projectFile);
                }
            });
        }
    }
}
