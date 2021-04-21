using BepInEx;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Chai
{
	[BepInDependency("com.bepis.r2api")]
	[BepInPlugin("com.chai.qualityOfRain", "Quality of Rain", "1.0.0")]
	public class QualityOfRain : BaseUnityPlugin
	{
		private const float RADIUS = 7f;
		private const float BIG_RADIUS = 11.5f;

		public void Awake()
		{
			On.RoR2.Stage.Start += (orig, self) =>
			{
				orig(self);
				if (NetworkServer.active)
				{
					typeof(EntityStates.Duplicator.Duplicating).SetFieldValue("initialDelayDuration", 0f);
					typeof(EntityStates.Duplicator.Duplicating).SetFieldValue("timeBetweenStartAndDropDroplet", 0f);
					//typeof(EntityStates.Scrapper.WaitToBeginScrapping).SetFieldValue("duration", 0f);
					typeof(EntityStates.Scrapper.Scrapping).SetFieldValue("duration", 0f);
					typeof(EntityStates.Scrapper.ScrappingToIdle).SetFieldValue("duration", 0.25f);
				}
			};

			On.EntityStates.Duplicator.Duplicating.BeginCooking += (orig, self) =>
			{
				if (!NetworkServer.active) { orig(self); }
			};

			On.EntityStates.Duplicator.Duplicating.DropDroplet += (orig, self) =>
			{
				orig(self);
				if (NetworkServer.active)
				{
					self.outer.GetComponent<PurchaseInteraction>().Networkavailable = true;
				}
			};

			On.RoR2.ShrineChanceBehavior.AddShrineStack += (orig, self, activator) =>
			{
				orig(self, activator);
				self.SetFieldValue("refreshTimer", 0f);
			};

			On.RoR2.GenericPickupController.GrantLunarCoin += (orig, self, body, count) =>
			{
				orig(self, body, count);

				foreach (CharacterMaster cm in CharacterMaster.readOnlyInstancesList)
				{
					NetworkUser networkUser = Util.LookUpBodyNetworkUser(cm.GetBody());
					if (cm.GetBody() == body || networkUser == null) { continue; }
					networkUser.AwardLunarCoins(count);
				}
			};

			On.RoR2.TeleporterInteraction.OnInteractionBegin += (orig, self, activator) =>
			{
				orig(self, activator);
				if (!self.isIdle) { return; }

				List<CharacterMaster> turrets = new List<CharacterMaster>();

				foreach (CharacterMaster cm in CharacterMaster.readOnlyInstancesList)
				{
					if (cm.name.StartsWith("Turret1"))
					{
						turrets.Add(cm);
					}
				}

				float angle = 2 * Mathf.PI / turrets.Count;
				bool primordial = self.name.StartsWith("Lunar"); //(Run.instance.stageClearCount + 1) % 5 == 0;
				float radius = primordial ? BIG_RADIUS : RADIUS;
				for (int i = 0; i < turrets.Count; ++i)
				{
					Vector3 point = new Vector3(Mathf.Cos(angle * i) * radius, 10f, Mathf.Sin(angle * i) * radius);

					if (Physics.Raycast(self.transform.position + point, Vector3.down, out RaycastHit hit))
					{
						point = hit.point;
						point.y += 0.1f;
						TeleportHelper.TeleportBody(turrets[i].GetBody(), point);
					}
				}
			};
		}
	}
}