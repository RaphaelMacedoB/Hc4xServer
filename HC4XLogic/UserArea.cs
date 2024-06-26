﻿using HC4x_Server.HCStone;
using HC4x_Server.Logic;
using HC4xServer.Core;
using HC4xServer.Logic;
using LibModel;
using LibServer;
using System;
using System.Threading.Tasks;

namespace HC4x_Server.PrivateArea
{
  internal class UserArea : RawPage
  {
    private const string Name = nameof(UserArea);
    #region Axis
    private new PageCore axMundi => (PageCore)base.axMundi;
    #endregion
    #region Node
    private HC4x_SectorUser scUser { get; set; }
    private HC4x_SectorCustomer scCustomer { get; set; }
    private HC4x_NodeUser ndUser { get; set; }
    private HC4x_NodeCustomer ndCustomer { get; set; }
    #endregion
    #region Method
    internal bool GetHcStoneCatalog(int parPkeyCustomer)
    {
      bool retValue = false;
      RawTable objTable;
      RawRow[] arNode;
      view_stone_catalog Hc4xViewStoneCatalog;
      try
      {
        objTable = scData.SelectCommand("nameCustomer,logoCustomer,productCover,description", "hc4xcustomer LEFT JOIN stoneproduct ON hc4xcustomer.pkeyCustomer = stoneproduct.pkeyCustomer", $"hc4xcustomer.pkeyCustomer = {parPkeyCustomer}", "");
        arNode = objTable.scRow.ArrayNode();
        Hc4xViewStoneCatalog = new view_stone_catalog(axMundi);
        ndCurInterface.atHeader.Replace("{hc4x-key:logoCustomer}", arNode[0].ValueStr("logoCustomer"));
        ndCurInterface.atHeader.Replace("{hc4x-key:nameCustomer}", arNode[0].ValueStr("nameCustomer"));
        retValue = Hc4xViewStoneCatalog.public_catalog(ndCurInterface, "3");
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(GetHcStoneCatalog)); }
      return retValue;
    }
    internal bool ValidCPFCNPJ(string parCPForCNPJ)
    {
      bool retValue = true;
      if (parCPForCNPJ.Length == 14)
        retValue = HC4x_SectorCustomer.ValidCPF(parCPForCNPJ);
      else
        retValue = HC4x_SectorCustomer.ValidCNPJ(parCPForCNPJ);
      return retValue;
    }

    internal bool AreaUser()
    {
      bool retValue = false;
      try
      {
        if (axRequest.FileKey() != null && axRequest.FileKey().Length > 0)
          UpdateProfilePic("upload-image-profile", scUser.UpdateImg, ndUser);
        retValue = UpdateUserForm(ndUser);
        ndCurInterface.EvalForm(ndUser);
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(AreaUser)); }
      return (retValue);
    }
    internal bool UpdateProfilePic(string uploadFolderPath, Func<object, bool> updateMethod, object updateObject)
    {
      bool retValue = false;
      string strWwwPath;
      NodeFormFile[] arFormFile;
      string imagePath;
      Task<bool> objTask;

      try
      {
        arFormFile = axRequest.FileKey();
        strWwwPath = GearPath.Combine(axMundi.atWebPath, uploadFolderPath);
        foreach (NodeFormFile itFile in arFormFile)
        {
          strWwwPath = itFile.GetSafeName(strWwwPath);
          imagePath = GearPath.Combine("/" + uploadFolderPath, GearPath.FileName(itFile.GetSafeName(strWwwPath)));
          if (updateObject is HC4x_NodeUser user)
            user.atImg = imagePath.Replace("\\", "/");
          if (updateObject is HC4x_NodeCustomer customer)
            customer.atLogoCustomer = imagePath.Replace("\\", "/");
          objTask = Task.Run(() => itFile.SaveLocalServer(strWwwPath));
          if (!objTask.Result) break;
        }
        retValue = updateMethod(updateObject);
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(UpdateProfilePic)); }
      return retValue;
    }

    internal bool GetImgUser()
    {
      bool retValue = false;
      string atPath = scUser.FindImgUser(ndUser.atId);
      if (atPath != "")
      {
        retValue = true;
      }
      else
      {
        ndUser.atImg = "/images/ico/user-default.png";
      }
      retValue = scUser.UpdateImg(ndUser);
      ndCurInterface.EvalForm(ndUser);
      return retValue;
    }
    internal bool RegisterCustomer()
    {
      bool retValue = false;
      PostCustomer objPostCustomer;
      HC4x_NodeCustomer objCustomer;
      HC4x_NodeAppUser objAppUser;
      int appUserId;
      try
      {
        objPostCustomer = axRequest.FormKeyVal<PostCustomer>();
        objCustomer = new HC4x_NodeCustomer(axMundi);
        objAppUser = new HC4x_NodeAppUser(axMundi);
        if (ValidCPFCNPJ(objPostCustomer.atCnpjCpf))
        {
          objCustomer.atCnpjCpf = objPostCustomer.atCnpjCpf;
        }
        else
        {
          atMessage = scCustomer.GetAlertByType(hc4x_TypeAlert.Danger, $"{objPostCustomer.atCnpjCpf} não é válido !");
          return ndCurInterface.EvalForm(objPostCustomer);
        }
        objAppUser.atPkeyUser = axSession.atPkUser;
        objAppUser.atPkeyApp = 4;
        appUserId = scCustomer.FindAppUserById(axSession.atPkUser);
        if (appUserId < 0)
          objCustomer.atPkeyAppUser = scCustomer.dbInsertAppUser(objAppUser).atPkeyAppUser;
        else
          objCustomer.atPkeyAppUser = appUserId;
        objCustomer.atRazaoSocial = objPostCustomer.atRazaoSocial;
        objCustomer.atNameCustomer = objPostCustomer.atNameCustomer;
        objCustomer.atPkeyCustomerCategory = scCustomer.FindPkeyCustomerCategoryByCategory(objPostCustomer.atCustomerCategory);
        objCustomer.atNameContact = objPostCustomer.atNameContact;
        objCustomer.atEmailContact = objPostCustomer.atEmailContact;
        objCustomer.atSite = objPostCustomer.atSite;
        objCustomer.atDescCustomer = objPostCustomer.atDescCustomer;
        if (axRequest.FileKey() != null && axRequest.FileKey().Length > 0)
          UpdateProfilePic("upload-customer-profile", scCustomer.UpdateImg, objCustomer);
        scCustomer.dbInsertCustomer(objCustomer);
        retValue = ndCurInterface.EvalForm(objCustomer);
        atMessage = scCustomer.GetAlertByType(hc4x_TypeAlert.Success, "Cliente registrado com sucesso !");
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(RegisterCustomer)); }
      return (retValue);
    }
    internal bool UpdateUserForm(HC4x_NodeUser parUser)
    {
      bool retValue = false;
      HC4x_UserHandler objUserHander = new HC4x_UserHandler();
      RawMailMessage objMail;
      PostUser objPostUser;
      objPostUser = axRequest.FormKeyVal<PostUser>();

      if (!string.IsNullOrEmpty(objPostUser.atName))
        parUser.atDesc = objPostUser.atName;
      if (!string.IsNullOrEmpty(objPostUser.atMail) && objPostUser.atMail != parUser.atEmail)
      {
        parUser.atSelfUser = (int)hc4x_UserStatus.Waiting;
        parUser.atEmail = objPostUser.atMail;
        objUserHander.Init(axMundi);
        objMail = objUserHander.RegisterMail(parUser, hc4x_SiteArea.publicarea, "confirm-email_eml", "confirm-email");
        if (objMail != null) axResponse.SendMail(objMail);
      }
      if (retValue = scUser.UpdateUserData(parUser))
        atMessage = scCustomer.GetAlertByType(hc4x_TypeAlert.Success, "Dados do usuário alterados com sucesso !");
      return retValue;
    }
    internal bool UpdateCustomerForm(HC4x_NodeCustomer parCustomer)
    {
      bool retValue = false;
      PostCustomer objPostCustomer;
      objPostCustomer = axRequest.FormKeyVal<PostCustomer>();
      parCustomer.atNameCustomer = objPostCustomer.atNameCustomer;
      parCustomer.atPkeyCustomerCategory = scCustomer.FindPkeyCustomerCategoryByCategory(objPostCustomer.atCustomerCategory);
      parCustomer.atRazaoSocial = objPostCustomer.atRazaoSocial;
      parCustomer.atCnpjCpf = objPostCustomer.atCnpjCpf;
      parCustomer.atNameContact = objPostCustomer.atNameContact;
      parCustomer.atEmailContact = objPostCustomer.atEmailContact;
      parCustomer.atSite = objPostCustomer.atSite;
      parCustomer.atDescCustomer = objPostCustomer.atDescCustomer;
      if (axRequest.FileKey() != null && axRequest.FileKey().Length > 0)
        UpdateProfilePic("upload-customer-profile", scCustomer.UpdateImg, ndCustomer);
      if (retValue = scCustomer.UpdateCustomerData(parCustomer))
        atMessage = scCustomer.GetAlertByType(hc4x_TypeAlert.Success, "Dados do cliente alterados com sucesso !");
      return retValue;
    }
    #endregion
    #region RawPage
    public override bool ActionRender()
    {
      bool retValue = false;
      try
      {
        if (ndCurInterface.EvalForm(ndUser))
          retValue = base.ActionRender();
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(ActionRender)); }
      return (retValue);
    }
    public override bool ActionGet(string parPageId)
    {
      bool retValue = false;
      ndUser = scUser.FindUserById(axSession.atPkUser);
      ndCustomer = scCustomer.FindCustomerByPkeyUser(axSession.atPkUser);
      switch (parPageId)
      {
        case c_area_cliente:
          retValue = GetImgUser();
          break;
        case c_register_customer:
          if (ndCustomer == null)
            retValue = base.ActionRender();
          else retValue = ndCurInterface.EvalForm(ndCustomer);
          break;
        case c_hctsone_catalogo:
          if (ndCustomer == null)
          {
            axMundi.RedirectTo(hc4x_SiteArea.privatearea, "register-customer");
            retValue = true;
          }
          else
            retValue = GetHcStoneCatalog(ndCustomer.atPkeyCustomer);
          break;
        default:
          retValue = true;
          break;
      }
      return retValue;
    }
    public override bool ActionPost(string parPageId)
    {
      bool retValue = false;
      ndUser = scUser.FindUserById(axSession.atPkUser);
      ndCustomer = scCustomer.FindCustomerByPkeyUser(axSession.atPkUser);
      try
      {
        switch (parPageId)
        {
          case c_area_cliente:
            retValue = AreaUser();
            break;
          case c_register_customer:
            if (ndCustomer == null)
              retValue = RegisterCustomer();
            else
            {
              UpdateCustomerForm(ndCustomer);
              retValue = ndCurInterface.EvalForm(ndCustomer);
            }
            break;
        }
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(ActionPost)); }
      return (retValue);
    }
    #endregion
    #region Constructor
    public override bool Init(AxisMundi parMundi)
    {
      bool retValue = false;
      try
      {
        if (base.Init(parMundi))
        {
          scUser = new HC4x_SectorUser(axMundi);
          scCustomer = new HC4x_SectorCustomer(axMundi);
          retValue = scUser.Init() && scCustomer.Init();
        }
      }
      catch (Exception Err) { axMundi.ShowException(Err, Name, nameof(Init)); }
      return (retValue);
    }
    #endregion
    #region Constant
    private const string c_area_cliente = "area-usuario";
    private const string c_register_customer = "register-customer";
    private const string c_hctsone_catalogo = "hcstonecatalogo";
    #endregion
  }
}