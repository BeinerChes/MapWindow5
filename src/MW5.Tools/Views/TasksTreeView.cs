﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MW5.Plugins.Interfaces;
using MW5.Tools.Enums;
using MW5.Tools.Model;
using MW5.Tools.Model.Parameters;
using MW5.Tools.Properties;
using MW5.UI.Controls;
using Syncfusion.Windows.Forms.Tools;

namespace MW5.Tools.Views
{
    public partial class TasksTreeView : TreeViewBase
    {
        private ITaskCollection _tasks;

        public TasksTreeView()
        {
            InitializeComponent();

            ShowSuperTooltip = false;
        }

        protected override IEnumerable<Bitmap> OnCreateImageList()
        {
            yield return Resources.img_wait16;
            yield return Resources.img_ok16;
            yield return Resources.img_error16;
            yield return Resources.img_database16;
            yield return Resources.img_result16;
            yield return Resources.img_options24;
            yield return Resources.img_warning16;
        }

        public void Initialize(ITaskCollection tasks)
        {
            if (tasks == null) throw new ArgumentNullException("tasks");
            _tasks = tasks;
            _tasks.CollectionChanged += (s, e) => PopulateTree();
        }

        private void PopulateTree()
        {
            Nodes.Clear();

            foreach (var task in _tasks)
            {
                var nodeTask = new TreeNodeAdv(task.Tool.Name)
                                   {
                                       Tag = task, 
                                       LeftImageIndices = new[] { (int)TaskIcons.Success }
                                   };

                var tool = task.Tool as GisToolBase;
                if (tool != null)
                {
                    AddToolParameters(nodeTask, tool, false);

                    AddToolParameters(nodeTask, tool, true);
                }

                AddTaskExecutionStats(nodeTask, task);

                bool hasErrors = false;
                if (hasErrors)
                {
                    var nodeErrors = new TreeNodeAdv("Errors") { LeftImageIndices = new[] { (int)TaskIcons.Log } };
                    nodeTask.Nodes.Add(nodeErrors);
                }

                Nodes.Add(nodeTask);

                nodeTask.ExpandAll();
            }
        }

        private void AddTaskExecutionStats(TreeNodeAdv nodeTask, IGisTask task)
        {
            var nodeExecution = new TreeNodeAdv("Execution") { LeftImageIndices = new[] { (int)TaskIcons.Execution } };

            var list = new List<string>()
            {
                "Started at: " + task.StartTime,
                "Finished at: " + task.FinishTime,
                "Execution time: " + task.ExecutionTime,
                "Status: " + task.Status
            };

            foreach (var s in list)
            {
                nodeExecution.Nodes.Add(new TreeNodeAdv(s));
            }

            nodeTask.Nodes.Add(nodeExecution);
        }

        private void AddToolParameters(TreeNodeAdv nodeTask, GisToolBase tool, bool output)
        {
            var nodeParameters = new TreeNodeAdv(output ? "Output" : "Input");
            nodeParameters.LeftImageIndices = new[] { (int) (output ? TaskIcons.Result : TaskIcons.Input) };

            foreach (var p in tool.Parameters)
            {
                bool outputParameter = p is OutputLayerParameter;
                if (outputParameter != output)
                {
                    continue;
                }

                // TODO: use multiline description
                var node = new TreeNodeAdv(p.ToString());
                nodeParameters.Nodes.Add(node);
            }

            nodeTask.Nodes.Add(nodeParameters);
        }

        public void UpdateView()
        {
            PopulateTree();
        }
    }
}
