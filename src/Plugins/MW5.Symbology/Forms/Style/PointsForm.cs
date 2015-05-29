﻿// ********************************************************************************************************
// <copyright file="MWLite.Symbology.cs" company="MapWindow.org">
// Copyright (c) MapWindow.org. All rights reserved.
// </copyright>
// The contents of this file are subject to the Mozilla Public License Version 1.1 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http:// Www.mozilla.org/MPL/ 
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specificlanguage governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this version of the Original Code is Sergei Leschinski
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date            Changed By      Notes
// ********************************************************************************************************

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using MW5.Api.Concrete;
using MW5.Api.Enums;
using MW5.Api.Interfaces;
using MW5.Api.Legend;
using MW5.Api.Legend.Abstract;
using MW5.Plugins.Symbology.Services;
using MW5.Shared;
using MW5.UI.Enums;
using MW5.UI.Forms;

namespace MW5.Plugins.Symbology.Forms.Style
{
    public partial class PointsForm : MapWindowForm
    {
        private static int _tabIndex;

        private readonly IMuteLegend _legend;
        private readonly ILegendLayer _layer;
        private readonly IGeometryStyle _style;
        private bool _noEvents;
        private string _initState;

        #region Initialization

        /// <summary>
        /// Creates a new instance of PointsForm class
        /// </summary>
        public PointsForm(IMuteLegend legend, ILegendLayer layer, IGeometryStyle options, bool applyDisabled)
        {
            if (layer == null) throw new ArgumentNullException("layer");
            if (options == null) throw new ArgumentNullException("options");
            if (legend == null) throw new ArgumentNullException("legend");

            InitializeComponent();
            
            _legend = legend;
            _layer = layer;
            _style = options;

            // setting values to the controls
            _initState = _style.Serialize();
            _noEvents = true;

            btnApply.Visible = !applyDisabled;

            pointIconControl1.Initialize(_style.Marker);

            InitControls();

            Options2Gui();
            _noEvents = false;

            InitFonts();

            DrawPreview();

            AttachListeners();

            UpdateDefaultColor();

            tabControl1.SelectedIndex = _tabIndex;
        }

        private void InitControls()
        {
            icbPointShape.ComboStyle = ImageComboStyle.PointShape;
            icbLineType.ComboStyle = ImageComboStyle.LineStyle;
            icbLineWidth.ComboStyle = ImageComboStyle.LineWidth;
            icbHatchStyle.ComboStyle = ImageComboStyle.HatchStyle;

            pnlFillPicture.Parent = groupBox3;    // options
            pnlFillPicture.Top = pnlFillHatch.Top;
            pnlFillPicture.Left = pnlFillHatch.Left;

            pnlFillGradient.Parent = groupBox3;    // options
            pnlFillGradient.Top = pnlFillHatch.Top;
            pnlFillGradient.Left = pnlFillHatch.Left;

            cboFillType.Items.Clear();
            cboFillType.Items.Add("Solid");
            cboFillType.Items.Add("Hatch");
            cboFillType.Items.Add("Gradient");

            cboGradientType.Items.Clear();
            cboGradientType.Items.Add("Linear");
            cboGradientType.Items.Add("Retangular");
            cboGradientType.Items.Add("Circle");
        }

        private void InitFonts()
        {
            var marker = _style.Marker;

            cboFontName.SelectedIndexChanged += cboFontName_SelectedIndexChanged;
            RefreshFontList(null, null);
            characterControl1.SelectedCharacterCode = (byte)marker.FontCharacter;
        }

        private void AttachListeners()
        {
            clpFillColor.SelectedColorChanged += clpFillColor_SelectedColorChanged;
            cboFillType.SelectedIndexChanged += cboFillType_SelectedIndexChanged;

            udRotation.ValueChanged += Gui2Options;
            udPointNumSides.ValueChanged += Gui2Options;
            udSideRatio.ValueChanged += Gui2Options;
            udSize.ValueChanged += Gui2Options;
            chkShowAllFonts.CheckedChanged += RefreshFontList;

            // line
            chkOutlineVisible.CheckedChanged += Gui2Options;
            icbLineType.SelectedIndexChanged += Gui2Options;
            icbLineWidth.SelectedIndexChanged += Gui2Options;
            clpOutline.SelectedColorChanged += clpOutline_SelectedColorChanged;

            chkFillVisible.CheckedChanged += Gui2Options;

            pointIconControl1.SelectedIconChanged += IconControl1SelectionChanged;
            pointIconControl1.ScaleChanged += () => Gui2Options(null, null);

            // character
            characterControl1.SelectionChanged += characterControl1_SelectionChanged;
            symbolControl1.SelectionChanged += SymbolControl1SelectionChanged;

            // hatch
            icbHatchStyle.SelectedIndexChanged += Gui2Options;
            chkFillBgTransparent.CheckedChanged += Gui2Options;
            clpHatchBack.SelectedColorChanged += Gui2Options;

            // gradient
            clpGradient2.SelectedColorChanged += Gui2Options;
            udGradientRotation.ValueChanged += Gui2Options;
            cboGradientType.SelectedIndexChanged += Gui2Options;
        }

        #endregion

        #region Properties

        private SymbologyMetadata Metadata
        {
            get { return SymbologyPlugin.GetMetadata(_layer.Handle); }
        }

        #endregion

        #region Font characters
        /// <summary>
        /// Refreshes the list of fonts
        /// </summary>
        private void RefreshFontList(object sender, EventArgs e)
        {
            cboFontName.Items.Clear();

            if (!chkShowAllFonts.Checked)
            {
                foreach (FontFamily family in FontFamily.Families)
                {
                    string name = family.Name.ToLower();

                    if (name == "webdings" ||
                        name == "wingdings" ||
                        name == "wingdings 2" ||
                        name == "wingdings 3" ||
                        name == "times new roman")
                    {
                        cboFontName.Items.Add(family.Name);
                    }
                }
            }
            else
            {
                foreach (FontFamily family in FontFamily.Families)
                {
                    cboFontName.Items.Add(family.Name);
                }
            }

            RestoreSelectedFont();
        }

        private void RestoreSelectedFont()
        {
            var fontName = _style.Marker.FontName;

            foreach (var item in cboFontName.Items)
            {
                if (item.ToString().EqualsIgnoreCase(fontName))
                {
                    cboFontName.SelectedItem = item;
                    break;
                }
            }

            if (cboFontName.SelectedIndex == -1)
            {
                cboFontName.SelectedItem = "Arial";
            }
        }

        /// <summary>
        /// Changing the font in the font control
        /// </summary>
        private void cboFontName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_noEvents)
            {
                btnApply.Enabled = true;
            }

            characterControl1.SetFontName(cboFontName.Text);
            _style.Marker.FontName = cboFontName.Text;
            DrawPreview();
        }

        /// <summary>
        /// Updates the preview with the newly selected character
        /// </summary>
        void characterControl1_SelectionChanged()
        {
            if (!_noEvents)
            {
                btnApply.Enabled = true;
            }

            _style.Marker.Type = MarkerType.FontCharacter;
            _style.Marker.FontCharacter = Convert.ToChar(characterControl1.SelectedCharacterCode);
            DrawPreview();
        }

        #endregion

        #region Preview

        /// <summary>
        /// Draws preview based on the chosen options
        /// </summary>
        private void DrawPreview()
        {
            if (_noEvents)
            {
                return;
            }

            if (pctPreview.Image != null)
            {
                pctPreview.Image.Dispose();
            }

            var rect = pctPreview.ClientRectangle;
            var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bmp);
            
            _style.DrawPoint(g, 0.0f, 0.0f, rect.Width, rect.Height,  BackColor);
            
            pctPreview.Image = bmp;
        }

        #endregion

        #region Options -> UI

        /// <summary>
        /// Sets the values entered by user to the class
        /// </summary>
        private void Gui2Options(object sender, EventArgs e)
        {
            if (_noEvents)
            {
                return;
            }

            var fill = _style.Fill;
            var marker = _style.Marker;

            marker.Size = (float)udSize.Value;

            marker.UpdatePictureScale(pointIconControl1.ScaleIcons, (int)udSize.Value);

            marker.Rotation = (double)udRotation.Value;

            fill.Color =  clpFillColor.Color;

            marker.VectorMarker = (VectorMarkerType)icbPointShape.SelectedIndex;
            marker.VectorSideCount = (int)udPointNumSides.Value;
            marker.VectorMarkerSideRatio = (float)udSideRatio.Value / 10;
        
            _style.Line.DashStyle = (DashStyle)icbLineType.SelectedIndex;
            _style.Line.Width = (float)icbLineWidth.SelectedIndex + 1;
            _style.Line.Visible = chkOutlineVisible.Checked;
            fill.Visible = chkFillVisible.Checked;
            fill.Type = (FillType)cboFillType.SelectedIndex;

            // hatch
            fill.HatchStyle = (HatchStyle)icbHatchStyle.SelectedIndex;
            fill.BgTransparent = chkFillBgTransparent.Checked;
            fill.BgColor =  clpHatchBack.Color;

            // gradient
            fill.GradientType = (GradientType)cboGradientType.SelectedIndex;
            fill.Color2 =  clpGradient2.Color;
            fill.Rotation = (double)udGradientRotation.Value;

            fill.Transparency = transparencyControl1.Value;
            _style.Line.Transparency = transparencyControl1.Value;

            if (!_noEvents)
            {
                btnApply.Enabled = true;
            }

            DrawPreview();
        }

        /// <summary>
        /// Loads the values of the class instance to the controls
        /// </summary>
        private void Options2Gui()
        {
            _noEvents = true;

            var marker = _style.Marker;
            udSize.SetValue(marker.Size);
            udRotation.SetValue(marker.Rotation);
            clpFillColor.Color =  _style.Fill.Color;

            // point
            icbPointShape.SelectedIndex = (int)marker.VectorMarker;
            udPointNumSides.SetValue(marker.VectorSideCount);
            udSideRatio.SetValue(marker.VectorMarkerSideRatio * 10.0);
            
            // options
            icbLineType.SelectedIndex = (int)_style.Line.DashStyle;
            icbLineWidth.SelectedIndex = (int)_style.Line.Width - 1;
            cboFillType.SelectedIndex = (int)_style.Fill.Type;
            chkOutlineVisible.Checked = _style.Line.Visible;
            clpOutline.Color =  _style.Line.Color;
            chkFillVisible.Checked = _style.Fill.Visible;
            
            // hatch
            icbHatchStyle.SelectedIndex = (int)_style.Fill.HatchStyle;
            chkFillBgTransparent.Checked = _style.Fill.BgTransparent;
            clpHatchBack.Color = _style.Fill.BgColor;

            // gradient
            cboGradientType.SelectedIndex = (int)_style.Fill.GradientType;
            clpGradient2.Color =  _style.Fill.Color2;
            udGradientRotation.Value = (decimal)_style.Fill.Rotation;

            transparencyControl1.Value = _style.Fill.Transparency;

            _noEvents = false;
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Toggles fill type oprions
        /// </summary>
        private void cboFillType_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlFillGradient.Visible = false;
            pnlFillHatch.Visible = false;
            pnlFillPicture.Visible = false;
            lblNoOptions.Visible = false;

            var fill = _style.Fill;
            if (cboFillType.SelectedIndex == (int)FillType.Hatch)
            {
                pnlFillHatch.Visible = true;
                fill.Type = FillType.Hatch;
            }
            else if (cboFillType.SelectedIndex == (int)FillType.Gradient)
            {
                pnlFillGradient.Visible = true;
                fill.Type = FillType.Gradient;
            }
            else if (cboFillType.SelectedIndex == (int)FillType.Picture)
            {
                pnlFillPicture.Visible = true;
                fill.Type = FillType.Picture;
            }
            else
            {
                lblNoOptions.Visible = true;
                fill.Type = FillType.Solid;
            }

            if (!_noEvents)
            {
                btnApply.Enabled = true;
            }

            DrawPreview();
        }

        /// <summary>
        /// Updates the preview with newly selected icon
        /// </summary>
        private void IconControl1SelectionChanged()
        {
            var filename = pointIconControl1.SelectedIconPath;
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                return;
            }

            var clrTransparent = Color.White;

            var img = BitmapSource.Open(filename, true);
            {
                img.TransparentColorFrom = clrTransparent;
                img.TransparentColorTo = clrTransparent;
                img.UseTransparentColor = true;

                var marker = _style.Marker;
                marker.Type = MarkerType.Bitmap;
                marker.Icon = img;
                marker.UpdatePictureScale(pointIconControl1.ScaleIcons, (int)udSize.Value);

                DrawPreview();
            }

            if (!_noEvents)
            {
                btnApply.Enabled = true;
            }
        }

        /// <summary>
        /// Updates all the controls with the selected fill color
        /// </summary>
        private void clpFillColor_SelectedColorChanged(object sender, EventArgs e)
        {
            _style.Fill.Color =  clpFillColor.Color;

            UpdateDefaultColor();

            if (!_noEvents) btnApply.Enabled = true;

            DrawPreview();
        }

        /// <summary>
        ///  Updates all the control with the selected outline color
        /// </summary>
        private void clpOutline_SelectedColorChanged(object sender, EventArgs e)
        {
            _style.Line.Color =  clpOutline.Color;
            
            UpdateDefaultColor();

            if (!_noEvents) btnApply.Enabled = true;

            DrawPreview();
        }

        private void UpdateDefaultColor()
        {
            symbolControl1.ForeColor = clpFillColor.Color;
            characterControl1.ForeColor = clpFillColor.Color;
            icbPointShape.Color1 = clpFillColor.Color;
        }

        /// <summary>
        /// Changes the transparency
        /// </summary>
        private void transparencyControl1_ValueChanged(object sender, byte value)
        {
            Gui2Options(null, null);
        }

        /// <summary>
        /// Changes the chosen point symbol
        /// </summary>
        private void SymbolControl1SelectionChanged()
        {
            var symbol = (VectorMarker)symbolControl1.SelectedIndex;
            _style.Marker.SetVectorMarker(symbol);

            if (!_noEvents)
            {
                btnApply.Enabled = true;
            }

            Options2Gui();
            DrawPreview();
        }

        /// <summary>
        /// Saves the selected page
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            _tabIndex = tabControl1.SelectedIndex;

            if (_style.Serialize() != _initState)
            {
                //m_legend.FireLayerPropertiesChanged(m_layer.Handle);
                _legend.Redraw(LegendRedraw.LegendAndMap);
            }
        }

        /// <summary>
        /// Saves options and redraws map without closing the form
        /// </summary>
        private void OnApplyClick(object sender, EventArgs e)
        {
            //m_legend.FireLayerPropertiesChanged(m_layer.Handle);
            _legend.Redraw(LegendRedraw.LegendAndMap);
            btnApply.Enabled = false;
            _initState = _style.Serialize();
        }

        /// <summary>
        /// Reverts changes and closes the form
        /// </summary>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.Cancel)
            {
                _tabIndex = tabControl1.SelectedIndex;
                _style.Deserialize(_initState);
            }
        }

        #endregion
    }
}
