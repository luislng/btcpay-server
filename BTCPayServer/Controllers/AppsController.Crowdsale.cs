﻿using System;
using System.Threading.Tasks;
using BTCPayServer.Models.AppViewModels;
using BTCPayServer.Services.Apps;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Controllers
{
    public partial class AppsController
    {
        public class CrowdfundAppUpdated
        {
            public string AppId { get; set; }
        }
        
        public class CrowdfundSettings
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public bool Enabled { get; set; }
        
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        
            public string TargetCurrency { get; set; }
            public decimal? TargetAmount { get; set; }
        
            public bool EnforceTargetAmount { get; set; }
            public string CustomCSSLink { get; set; }
            public string MainImageUrl { get; set; }
            public string NotificationUrl { get; set; }
            public string Tagline { get; set; }
            public string EmbeddedCSS { get; set; }
            public string PerksTemplate { get; set; }
            public bool DisqusEnabled { get; set; }
            public bool SoundsEnabled { get; set; }
            public string DisqusShortname { get; set; }
            public bool AnimationsEnabled { get; set; }
            public bool UseInvoiceAmount { get; set; }
            public int ResetEveryAmount { get; set; }
            public CrowdfundResetEvery ResetEvery { get; set; }
        }
        
        
        [HttpGet]
        [Route("{appId}/settings/crowdfund")]
        public async Task<IActionResult> UpdateCrowdfund(string appId)
        {
            var app = await GetOwnedApp(appId, AppType.Crowdfund);
            if (app == null)
                return NotFound();
            var settings = app.GetSettings<CrowdfundSettings>();
            var vm = new UpdateCrowdfundViewModel()
            {
                Title = settings.Title,
                Enabled = settings.Enabled,
                EnforceTargetAmount = settings.EnforceTargetAmount,
                StartDate = settings.StartDate,
                TargetCurrency = settings.TargetCurrency,
                Description = settings.Description,
                MainImageUrl = settings.MainImageUrl,
                EmbeddedCSS = settings.EmbeddedCSS,
                EndDate = settings.EndDate,
                TargetAmount = settings.TargetAmount,
                CustomCSSLink = settings.CustomCSSLink,
                NotificationUrl = settings.NotificationUrl,
                Tagline = settings.Tagline,
                PerksTemplate = settings.PerksTemplate,
                DisqusEnabled = settings.DisqusEnabled,
                SoundsEnabled = settings.SoundsEnabled,
                DisqusShortname = settings.DisqusShortname,
                AnimationsEnabled = settings.AnimationsEnabled,
                UseInvoiceAmount = settings.UseInvoiceAmount,
                ResetEveryAmount = settings.ResetEveryAmount,
                ResetEvery = Enum.GetName(typeof(CrowdfundResetEvery), settings.ResetEvery),
            };
            return View(vm);
        }
        [HttpPost]
        [Route("{appId}/settings/crowdfund")]
        public async Task<IActionResult> UpdateCrowdfund(string appId, UpdateCrowdfundViewModel vm)
        {
            if (_AppsHelper.GetCurrencyData(vm.TargetCurrency, false) == null)
                ModelState.AddModelError(nameof(vm.TargetCurrency), "Invalid currency");
          
            try
            {
                _AppsHelper.Parse(vm.PerksTemplate, vm.TargetCurrency);
            }
            catch
            {
                ModelState.AddModelError(nameof(vm.PerksTemplate), "Invalid template");
            }
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            
            
            var app = await GetOwnedApp(appId, AppType.Crowdfund);
            if (app == null)
                return NotFound();
            app.SetSettings(new CrowdfundSettings()
            {
                Title = vm.Title,
                Enabled = vm.Enabled,
                EnforceTargetAmount = vm.EnforceTargetAmount,
                StartDate = vm.StartDate,
                TargetCurrency = vm.TargetCurrency,
                Description = vm.Description,
                EndDate = vm.EndDate,
                TargetAmount = vm.TargetAmount,
                CustomCSSLink = vm.CustomCSSLink,
                MainImageUrl = vm.MainImageUrl,
                EmbeddedCSS = vm.EmbeddedCSS,
                NotificationUrl = vm.NotificationUrl,
                Tagline = vm.Tagline,
                PerksTemplate = vm.PerksTemplate,
                DisqusEnabled = vm.DisqusEnabled,
                SoundsEnabled = vm.SoundsEnabled,
                DisqusShortname = vm.DisqusShortname,
                AnimationsEnabled = vm.AnimationsEnabled,
                ResetEveryAmount = vm.ResetEveryAmount,
                ResetEvery = Enum.Parse<CrowdfundResetEvery>(vm.ResetEvery),
                UseInvoiceAmount = vm.UseInvoiceAmount
            });
            await UpdateAppSettings(app);
            _EventAggregator.Publish(new CrowdfundAppUpdated()
            {
                AppId = appId
            });
            StatusMessage = "App updated";
            return RedirectToAction(nameof(ListApps));
        }
    }
}
