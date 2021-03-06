﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using GraphX.PCL.Common.Enums;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
using GraphX.PCL.Logic.Models;
using GraphX.Controls;
using GraphX.Controls.Models;
using QuickGraph;
using System.Windows;
using System.Linq;

namespace VHDL_FSM_Visualizer
{
    public partial class Form1 : Form
    {
        //FSM Vars
        List<FSM_State> fsmStates = new List<FSM_State>();
        string vhdlFilePath;
        string[] vhdlFileLinesOfCode;

        //GraphX Vars
        private ZoomControl _zoomctrl;
        private FSMGraphArea _gArea;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            wpfHost.Child = GenerateWpfVisuals();
            _zoomctrl.ZoomToFill();
        }

        private UIElement GenerateWpfVisuals()
        {
            _zoomctrl = new ZoomControl();
            ZoomControl.SetViewFinderVisibility(_zoomctrl, Visibility.Visible);
            /* ENABLES WINFORMS HOSTING MODE --- >*/
            var logic = new GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>();
            _gArea = new FSMGraphArea
            {
                EnableWinFormsHostingMode = true,
                LogicCore = logic,
                EdgeLabelFactory = new DefaultEdgelabelFactory()
            };
            _gArea.ShowAllEdgesLabels(false);
            logic.Graph = GenerateGraph();
            logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog;
            logic.DefaultLayoutAlgorithmParams = logic.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.LinLog);
            //((LinLogLayoutParameters)logic.DefaultLayoutAlgorithmParams). = 100;
            logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA;
            logic.DefaultOverlapRemovalAlgorithmParams = logic.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            ((OverlapRemovalParameters)logic.DefaultOverlapRemovalAlgorithmParams).HorizontalGap = 50;
            ((OverlapRemovalParameters)logic.DefaultOverlapRemovalAlgorithmParams).VerticalGap = 50;
            logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
            logic.AsyncAlgorithmCompute = false;
            _zoomctrl.Content = _gArea;
            _gArea.RelayoutFinished += gArea_RelayoutFinished;


            var myResourceDictionary = new ResourceDictionary { Source = new Uri("Templates\\template.xaml", UriKind.Relative) };
            _zoomctrl.Resources.MergedDictionaries.Add(myResourceDictionary);

            return _zoomctrl;
        }

        void gArea_RelayoutFinished(object sender, EventArgs e)
        {
            _zoomctrl.ZoomToFill();
        }

        private FSMGraph GenerateGraph()
        {
            //FOR DETAILED EXPLANATION please see SimpleGraph example project
            var dataGraph = new FSMGraph();
            foreach(FSM_State state in fsmStates)
            {
                var dataVertex = new DataVertex(state.name);
                dataGraph.AddVertex(dataVertex);
            }
            var vlist = dataGraph.Vertices.ToList();
            for (int i = 0; i < vlist.Count; i++)
            {
                FSM_State stateDst = fsmStates[i];
                for (int j=0;j < vlist.Count; j++)
                {
                    FSM_State stateSrc = fsmStates[j];
                    if (stateSrc.next_states.ContainsKey(stateDst.name))
                    {
                        var dataEdge = new DataEdge(vlist[j], vlist[i]) { Text = string.Format("{0} -> {1}", vlist[j], vlist[i]) };
                        dataGraph.AddEdge(dataEdge);
                    }
                }
            }
            //Then create two edges optionaly defining Text property to show who are connected
            //var dataEdge = new DataEdge(vlist[0], vlist[1]) { Text = string.Format("{0} -> {1}", vlist[0], vlist[1]) };
            //dataGraph.AddEdge(dataEdge);
            //dataEdge = new DataEdge(vlist[2], vlist[0]) { Text = string.Format("{0} -> {1}", vlist[2], vlist[0]) };
            //dataGraph.AddEdge(dataEdge);


            //dataEdge = new DataEdge(vlist[1], vlist[2]) { Text = string.Format("{0} -> {1}", vlist[2], vlist[1]) };
            //dataGraph.AddEdge(dataEdge);
            return dataGraph;
        }

        private void loadFileBtn_Click(object sender, EventArgs e)
        {
            int size = -1;
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                toolStripProgressBar1.Visible = true;
                toolStripProgressBar1.Value = 0; //zero progress bar
                Cursor.Current = Cursors.WaitCursor; //make wait cursor
                vhdlFilePath = openFileDialog1.FileName;
                fileSystemWatcher1.Filter = openFileDialog1.SafeFileName;
                fileSystemWatcher1.Path = Path.GetDirectoryName(vhdlFilePath);
                try
                {
                    toolStripProgressBar1.Value = 10;

                    vhdlFileLinesOfCode = File.ReadAllLines(vhdlFilePath);

                    toolStripProgressBar1.Value = 30;

                    fsmStates = Utils.vhdlParseStatesDecleration(vhdlFileLinesOfCode, fsmTypeTxtBox.Text);
                    fsmStates = Utils.vhdlParseStatesTransitions(fsmStates, vhdlFileLinesOfCode,  currStateTxtBox.Text, nextStateTxtBox.Text);

                    if (fsmStates.Count > 0)
                    {
                        toolStripProgressBar1.Value = 70;

                        wpfHost.Child = GenerateWpfVisuals();
                        _zoomctrl.ZoomToFill();
                        refreshGraph();
                        Cursor.Current = Cursors.Default; // make default cursor
                        toolStripProgressBar1.Value = 100; //full progress bar
                        toolStripProgressBar1.Visible = false;
                    } else
                    {
                        
                        toolStripProgressBar1.Value = 0;
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Error while reading file");
                }
            }
        }

        private void refreshGraph()
        {
            _gArea.GenerateGraph(true);
            _gArea.SetVerticesDrag(true, true);
            _zoomctrl.ZoomToFill();
        }

        private void refreshGraphBtn_Click(object sender, EventArgs e)
        {
            refreshGraph();
        }

        private void fileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Value = 10;

            bool fileRead = false;
            while (!fileRead)
            {
                try {
                    vhdlFileLinesOfCode = File.ReadAllLines(vhdlFilePath);
                    fileRead = true;
                }catch(Exception)
                {
                    fileRead = false;
                }
            }

            if (fileRead)
            {
                toolStripProgressBar1.Value = 30;

                fsmStates = Utils.vhdlParseStatesDecleration(vhdlFileLinesOfCode, fsmTypeTxtBox.Text);

                toolStripProgressBar1.Value = 60;

                wpfHost.Child = GenerateWpfVisuals();
                _zoomctrl.ZoomToFill();
                
                toolStripProgressBar1.Value = 100;

                refreshGraph();
            }
            
        }
    }
}
