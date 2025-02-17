using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.Utils;
using DevExpress.Utils.Layout;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout.Utils;
using OSPSuite.Assets;
using OSPSuite.DataBinding;
using OSPSuite.DataBinding.DevExpress;
using OSPSuite.DataBinding.DevExpress.XtraGrid;
using OSPSuite.Presentation.Extensions;
using OSPSuite.Presentation.Views;
using OSPSuite.UI.Controls;
using OSPSuite.UI.Extensions;
using OSPSuite.UI.RepositoryItems;
using OSPSuite.UI.Services;
using OSPSuite.Utility.Extensions;
using PKSim.Assets;
using PKSim.Presentation.DTO;
using PKSim.Presentation.DTO.Individuals;
using PKSim.Presentation.Presenters.Individuals;
using PKSim.Presentation.Views.Individuals;
using PKSim.UI.Extensions;
using Padding = System.Windows.Forms.Padding;

namespace PKSim.UI.Views.Individuals
{
   public partial class IndividualSettingsView : BaseContainerUserControl, IIndividualSettingsView
   {
      private readonly IImageListRetriever _imageListRetriever;
      private readonly IToolTipCreator _toolTipCreator;
      private readonly UserLookAndFeel _lookAndFeel;
      private IIndividualSettingsPresenter _presenter;

      private readonly ScreenBinder<IndividualSettingsDTO> _settingsBinder = new ScreenBinder<IndividualSettingsDTO>();
      private readonly ScreenBinder<IndividualSettingsDTO> _parameterBinder = new ScreenBinder<IndividualSettingsDTO>();
      private readonly GridViewBinder<CategoryParameterValueVersionDTO> _gridParameterValueVersionsBinder;
      private readonly GridViewBinder<CategoryCalculationMethodDTO> _gridCalculationMethodsBinder;

      private readonly RepositoryItemComboBox _repositoryForParameterValueVersions;
      private readonly RepositoryItemComboBox _repositoryForCalculationMethods;
      private readonly ToolTipController _toolTipController;

      public bool Updating { get; private set; }

      public IndividualSettingsView(IImageListRetriever imageListRetriever, IToolTipCreator toolTipCreator, UserLookAndFeel lookAndFeel)
      {
         InitializeComponent();
         _toolTipController = new ToolTipController();
         _toolTipController.Initialize(imageListRetriever);
         _imageListRetriever = imageListRetriever;
         _toolTipCreator = toolTipCreator;
         _lookAndFeel = lookAndFeel;

         _gridParameterValueVersionsBinder = new GridViewBinder<CategoryParameterValueVersionDTO>(gridViewParameterValueVersions);
         _gridCalculationMethodsBinder = new GridViewBinder<CategoryCalculationMethodDTO>(gridViewCalculationMethods);
         _repositoryForParameterValueVersions = new UxRepositoryItemComboBox(gridViewParameterValueVersions);
         _repositoryForCalculationMethods = new UxRepositoryItemComboBox(gridViewCalculationMethods);

         gridViewCalculationMethods.AllowsFiltering = false;
         gridViewCalculationMethods.VertScrollVisibility = ScrollVisibility.Never;
         gridViewParameterValueVersions.ShowColumnHeaders = false;
         gridViewCalculationMethods.ShowColumnHeaders = false;
         gridViewCalculationMethods.ShowRowIndicator = false;
         gridViewParameterValueVersions.ShowRowIndicator = false;
         gridCalculationMethods.ToolTipController = _toolTipController;
      }

      public override void InitializeBinding()
      {
         gridViewParameterValueVersions.CustomRowFilter += (o, e) => customizedParameterValueVersionRowVisibility(e);
         gridViewCalculationMethods.CustomRowFilter += (o, e) => customizedCalculationMethodsRowVisibility(e);
         _toolTipController.GetActiveObjectInfo += onToolTipControllerGetActiveObjectInfo;

         _settingsBinder.Bind(dto => dto.Species)
            .To(cbSpecies)
            .WithImages(species => _imageListRetriever.ImageIndex(species.Icon))
            .WithValues(dto => _presenter.AllSpecies())
            .AndDisplays(species => species.DisplayName)
            .Changed += () => _presenter.SpeciesChanged();

         _settingsBinder.Bind(dto => dto.Population)
            .To(cbPopulation)
            .WithValues(dto => _presenter.PopulationsFor(dto.Species))
            .AndDisplays(pop => pop.DisplayName)
            .Changed += () => _presenter.PopulationChanged();

         _settingsBinder.Bind(dto => dto.Gender)
            .To(cbGender)
            .WithValues(dto => _presenter.GenderFor(dto.Population))
            .AndDisplays(gender => gender.DisplayName)
            .Changed += () => _presenter.GenderChanged();

         _gridParameterValueVersionsBinder.Bind(pvv => pvv.DisplayName)
            .AsReadOnly();

         _gridParameterValueVersionsBinder.Bind(pvv => pvv.ParameterValueVersion)
            .WithRepository(pvv => _repositoryForParameterValueVersions)
            .WithEditorConfiguration(updatePvvListForCategory)
            .WithShowButton(ShowButtonModeEnum.ShowAlways);
         _gridParameterValueVersionsBinder.Changed += settingsChanged;

         _gridCalculationMethodsBinder.Bind(cm => cm.DisplayName)
            .AsReadOnly();

         _gridCalculationMethodsBinder.Bind(cm => cm.CalculationMethod)
            .WithRepository(cm => _repositoryForCalculationMethods)
            .WithEditorConfiguration(updateCmListForCategory)
            .WithShowButton(ShowButtonModeEnum.ShowAlways);
         _gridCalculationMethodsBinder.Changed += settingsChanged;

         _parameterBinder.Bind(dto => dto.ParameterAge)
            .To(uxAge)
            .Changed += () => _presenter.AgeChanged();

         _parameterBinder.Bind(dto => dto.ParameterGestationalAge)
            .To(uxGestationalAge)
            .Changed += () => _presenter.GestationalAgeChanged();

         _parameterBinder.Bind(dto => dto.ParameterHeight).To(uxHeight);
         _parameterBinder.Bind(dto => dto.ParameterWeight).To(uxWeight);
         _parameterBinder.Bind(dto => dto.ParameterBMI).To(uxBMI);


         btnMeanValues.Click += (o, e) => this.DoWithinWaitCursor(() => OnEvent(_presenter.RetrieveMeanValues));

         RegisterValidationFor(_settingsBinder, settingsChanged, settingsChanged);
         RegisterValidationFor(_parameterBinder, settingsChanged, settingsChanged);
      }

      private void onToolTipControllerGetActiveObjectInfo(object sender, ToolTipControllerGetActiveObjectInfoEventArgs e)
      {
         var gridControl = e.SelectedControl as GridControl;
         var gridView = gridControl?.GetViewAt(e.ControlMousePosition) as GridView;
         if (gridView == null) return;

         CategoryCategoryItemDTO categoryCategoryItemDTO;
         if (gridView == gridViewCalculationMethods)
            categoryCategoryItemDTO = _gridCalculationMethodsBinder.ElementAt(e);
         else
            categoryCategoryItemDTO = _gridParameterValueVersionsBinder.ElementAt(e);

         if (categoryCategoryItemDTO == null)
            return;

         var superToolTip = _toolTipCreator.ToolTipFor(categoryCategoryItemDTO);
         e.Info = _toolTipCreator.ToolTipControlInfoFor(categoryCategoryItemDTO, superToolTip);
      }

      private void updateCmListForCategory(BaseEdit activeEditor, CategoryCalculationMethodDTO cm)
      {
         activeEditor.FillComboBoxEditorWith(_presenter.AllCalculationMethodsFor(cm.Category));
      }

      private void updatePvvListForCategory(BaseEdit activeEditor, CategoryParameterValueVersionDTO pvv)
      {
         activeEditor.FillComboBoxEditorWith(_presenter.AllParameterValueVersionsFor(pvv.Category));
      }

      private void customizedParameterValueVersionRowVisibility(RowFilterEventArgs e)
      {
         var parameterValueVersionDTO = _gridParameterValueVersionsBinder.SourceElementAt(e.ListSourceRow);
         e.Visible = _presenter.ShouldDisplayPvvCategory(parameterValueVersionDTO.Category);
         e.Handled = true;
      }

      private void customizedCalculationMethodsRowVisibility(RowFilterEventArgs e)
      {
         var calculationMethodDTO = _gridCalculationMethodsBinder.SourceElementAt(e.ListSourceRow);
         e.Visible = _presenter.ShouldDisplayCmCategory(calculationMethodDTO.Category);
         e.Handled = true;
      }

      public void AttachPresenter(IIndividualSettingsPresenter presenter)
      {
         _presenter = presenter;
      }

      public void BindToSettings(IndividualSettingsDTO individualSettingsDTO)
      {
         this.DoWithinLatch(() => _settingsBinder.BindToSource(individualSettingsDTO));
      }

      public void BindToParameters(IndividualSettingsDTO individualSettingsDTO)
      {
         this.DoWithinLatch(() =>
         {
            _parameterBinder.BindToSource(individualSettingsDTO);
            _gridCalculationMethodsBinder.BindToSource(individualSettingsDTO.CalculationMethods);
         });

         layoutItemCalculationMethods.AdjustGridViewHeight(gridViewCalculationMethods, layoutControl);

         settingsChanged();
      }

      public void BindToSubPopulation(IEnumerable<CategoryParameterValueVersionDTO> subPopulation)
      {
         _gridParameterValueVersionsBinder.BindToSource(subPopulation);
      }

      private void settingsChanged()
      {
         if (IsLatched) return;
         btnMeanValues.Enabled = !_parameterBinder.HasError;
         _presenter.ViewChanged();
      }

      public void RefreshAllIndividualList()

      {
         _settingsBinder.RefreshListElements();
         settingsChanged();
      }

      public bool AgeVisible
      {
         set => tablePanel.RowFor(uxAge).Visible = value;
         get => tablePanel.RowFor(uxAge).Visible;
      }

      public bool GestationalAgeVisible
      {
         set
         {
            tablePanel.RowFor(uxGestationalAge).Visible = value;
            labelAge.Text = (value ? PKSimConstants.UI.PostnatalAge : PKSimConstants.UI.Age).FormatForLabel();
         }
         get => tablePanel.RowFor(uxGestationalAge).Visible;
      }

      public void AddValueOriginView(IView view)
      {
         var control = view.DowncastTo<Control>();
         var row = tablePanel.RowFor(labelValueOrigin);
         var rowIndex = tablePanel.GetRow(labelValueOrigin);
         var colIndex = tablePanel.GetColumn(uxAge);
         tablePanel.Controls.Add(control);
         tablePanel.SetCell(control, rowIndex, colIndex);
         control.Margin = uxAge.Margin;
         row.Height = tablePanel.RowFor(uxAge).Height;
         control.Height = cbSpecies.Height;
      }

      public void AddDiseaseStateView(IView view)
      {
         panelDiseaseState.FillWith(view);
      }

      public void UpdateControlSizeAndVisibility(bool hasDiseaseState)
      {
         layoutItemPopulationProperties.AdjustTablePanelHeight(tablePanel, layoutControl);
         layoutGroupDiseaseState.Visibility = LayoutVisibilityConvertor.FromBoolean(hasDiseaseState);
      }

      public bool HeightAndBMIVisible
      {
         set
         {
            tablePanel.RowFor(uxHeight).Visible = value;
            tablePanel.RowFor(uxBMI).Visible = value;
         }
         get => tablePanel.RowFor(uxHeight).Visible;
      }

      public bool IsReadOnly
      {
         set
         {
            layoutControlGroupPopulationProperties.Enabled = !value;
            layoutControlGroupPopulationParameters.Enabled = layoutControlGroupPopulationProperties.Enabled;
            layoutGroupDiseaseState.Enabled = layoutControlGroupPopulationProperties.Enabled;
         }
         get => !layoutControlGroupPopulationProperties.Enabled;
      }

      public bool SpeciesVisible
      {
         set => layoutItemSpecies.Visibility = LayoutVisibilityConvertor.FromBoolean(value);
         get => LayoutVisibilityConvertor.ToBoolean(layoutItemSpecies.Visibility);
      }

      public void BeginUpdate()
      {
         Updating = true;
         layoutControl.BeginUpdate();
      }

      public void EndUpdate()
      {
         Updating = false;
         layoutControl.EndUpdate();
      }

      public override bool HasError => _settingsBinder.HasError || _parameterBinder.HasError;

      public override void InitializeResources()
      {
         base.InitializeResources();
         layoutItemPopulation.Text = PKSimConstants.UI.Population.FormatForLabel();
         layoutItemSpecies.Text = PKSimConstants.UI.Species.FormatForLabel();
         layoutItemGender.Text = PKSimConstants.UI.Gender.FormatForLabel();
         labelAge.Text = PKSimConstants.UI.Age.FormatForLabel();
         labelGestationalAge.Text = PKSimConstants.UI.GestationalAge.FormatForLabel();
         labelWeight.Text = PKSimConstants.UI.Weight.FormatForLabel();
         labelHeight.Text = PKSimConstants.UI.Height.FormatForLabel();
         labelBMI.Text = PKSimConstants.UI.BMI.FormatForLabel(checkCase: false);
         layoutItemSubPopulation.Text = PKSimConstants.UI.SubPopulation.FormatForLabel();
         layoutItemSubPopulation.Visibility = LayoutVisibilityConvertor.FromBoolean(false);
         layoutItemCalculationMethods.Text = PKSimConstants.UI.CalculationMethods.FormatForLabel();
         layoutControlGroupPopulationParameters.Text = PKSimConstants.UI.IndividualParameters;
         layoutControlGroupPopulationProperties.Text = PKSimConstants.UI.PopulationProperties;
         layoutGroupDiseaseState.Text = PKSimConstants.UI.DiseaseState;
         labelValueOrigin.Text = Captions.ValueOrigin.FormatForLabel();
         btnMeanValues.Text = PKSimConstants.UI.MeanValues;
         layoutControl.InitializeDisabledColors(_lookAndFeel);
         uxBMI.Enabled = false;
         cbSpecies.SetImages(_imageListRetriever);
         btnMeanValues.Margin = new Padding(btnMeanValues.Margin.Left, uxHeight.Margin.Top, btnMeanValues.Margin.Right, btnMeanValues.Margin.Bottom);
         tablePanel.LabelVertAlignment = LabelVertAlignment.Center;
      }

      public override string Caption => PKSimConstants.UI.Biometrics;

      public override ApplicationIcon ApplicationIcon => ApplicationIcons.Individual;

      public bool IsLatched { get; set; }
   }
}