﻿using BTCPayServer.Data;
using DBreeze;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq;

namespace BTCPayServer.Authentication
{
	public class TokenRepository
	{
		ApplicationDbContextFactory _Factory;
		public TokenRepository(ApplicationDbContextFactory dbFactory)
		{
			if(dbFactory == null)
				throw new ArgumentNullException(nameof(dbFactory));
			_Factory = dbFactory;
		}

		public async Task<BitTokenEntity[]> GetTokens(string sin)
		{
			using(var ctx = _Factory.CreateContext())
			{
				return (await ctx.PairedSINData
					.Where(p => p.SIN == sin)
					.ToListAsync())
					.Select(p => CreateTokenEntity(p))
					.ToArray();
			}
		}

		private BitTokenEntity CreateTokenEntity(PairedSINData data)
		{
			return new BitTokenEntity()
			{
				Label = data.Label,
				Facade = data.Facade,
				Value = data.Id,
				SIN = data.SIN,
				PairingTime = data.PairingTime,
				StoreId = data.StoreDataId
			};
		}

		public async Task<string> CreatePairingCodeAsync()
		{
			string pairingCodeId = Encoders.Base58.EncodeData(RandomUtils.GetBytes(6));
			using(var ctx = _Factory.CreateContext())
			{
				var now = DateTime.UtcNow;
				var expiration = DateTime.UtcNow + TimeSpan.FromMinutes(15);
				await ctx.PairingCodes.AddAsync(new PairingCodeData()
				{
					Id = pairingCodeId,
					DateCreated = now,
					Expiration = expiration,
					TokenValue = Encoders.Base58.EncodeData(RandomUtils.GetBytes(32))
				});
				await ctx.SaveChangesAsync();
			}
			return pairingCodeId;
		}

		public async Task<PairingCodeEntity> UpdatePairingCode(PairingCodeEntity pairingCodeEntity)
		{
			using(var ctx = _Factory.CreateContext())
			{
				var pairingCode = await ctx.PairingCodes.FindAsync(pairingCodeEntity.Id);
				pairingCode.Label = pairingCodeEntity.Label;
				pairingCode.Facade = pairingCodeEntity.Facade;
				await ctx.SaveChangesAsync();
				return CreatePairingCodeEntity(pairingCode);
			}
		}

		public async Task<bool> PairWithStoreAsync(string pairingCodeId, string storeId)
		{
			using(var ctx = _Factory.CreateContext())
			{
				var pairingCode = await ctx.PairingCodes.FindAsync(pairingCodeId);
				if(pairingCode == null || pairingCode.Expiration < DateTimeOffset.UtcNow)
					return false;
				pairingCode.StoreDataId = storeId;
				await ActivateIfComplete(ctx, pairingCode);
				await ctx.SaveChangesAsync();
			}
			return true;
		}

		public async Task<bool> PairWithSINAsync(string pairingCodeId, string sin)
		{
			using(var ctx = _Factory.CreateContext())
			{
				var pairingCode = await ctx.PairingCodes.FindAsync(pairingCodeId);
				if(pairingCode == null || pairingCode.Expiration < DateTimeOffset.UtcNow)
					return false;
				pairingCode.SIN = sin;
				await ActivateIfComplete(ctx, pairingCode);
				await ctx.SaveChangesAsync();
			}
			return true;
		}


		private async Task ActivateIfComplete(ApplicationDbContext ctx, PairingCodeData pairingCode)
		{
			if(!string.IsNullOrEmpty(pairingCode.SIN) && !string.IsNullOrEmpty(pairingCode.StoreDataId))
			{
				ctx.PairingCodes.Remove(pairingCode);
				await ctx.PairedSINData.AddAsync(new PairedSINData()
				{
					Id = pairingCode.TokenValue,
					PairingTime = DateTime.UtcNow,
					Facade = pairingCode.Facade,
					Label = pairingCode.Label,
					StoreDataId = pairingCode.StoreDataId,
					SIN = pairingCode.SIN
				});
			}
		}


		public async Task<BitTokenEntity[]> GetTokensByStoreIdAsync(string storeId)
		{
			using(var ctx = _Factory.CreateContext())
			{
				return (await ctx.PairedSINData.Where(p => p.StoreDataId == storeId).ToListAsync())
						.Select(c => CreateTokenEntity(c))
						.ToArray();
			}
		}

		public async Task<PairingCodeEntity> GetPairingAsync(string pairingCode)
		{
			using(var ctx = _Factory.CreateContext())
			{
				return CreatePairingCodeEntity(await ctx.PairingCodes.FindAsync(pairingCode));
			}
		}

		private PairingCodeEntity CreatePairingCodeEntity(PairingCodeData data)
		{
			return new PairingCodeEntity()
			{
				Facade = data.Facade,
				Id = data.Id,
				Label = data.Label,
				Expiration = data.Expiration,
				CreatedTime = data.DateCreated,
				TokenValue = data.TokenValue,
				SIN = data.SIN
			};
		}


		public async Task<bool> DeleteToken(string tokenId)
		{
			using(var ctx = _Factory.CreateContext())
			{
				var token = await ctx.PairedSINData.FindAsync(tokenId);
				if(token == null)
					return false;
				ctx.PairedSINData.Remove(token);
				await ctx.SaveChangesAsync();
				return true;
			}
		}

		public async Task<BitTokenEntity> GetToken(string tokenId)
		{
			using(var ctx = _Factory.CreateContext())
			{
				var token = await ctx.PairedSINData.FindAsync(tokenId);
				return CreateTokenEntity(token);
			}
		}

	}
}
