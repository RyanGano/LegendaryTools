using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Faithlife.Utility;
using LegendaryClientConsole.Utility;
using LegendaryService;
using static LegendaryService.GameService;

namespace LegendaryClientConsole
{
	internal class DatabaseInitializer
	{
		internal static async ValueTask InitializeDatabase(GameServiceClient client)
		{
			// The order of this creation is important because later items depend on earlier items already existing.
			var teams = await CreateTeams(client);
			var classes = await CreateClasses(client);
			await CreateGamePackages(client);

			var request = new GetGamePackagesRequest();
			request.Fields.AddRange(new[] { GamePackageField.Id, GamePackageField.Name, GamePackageField.CoverImage, GamePackageField.PackageType, GamePackageField.Allies });
			var basePackages = await client.GetGamePackagesAsync(request);

			var supportedPackages = basePackages.Packages.ToList();

			var abilities = await CreateAbilities(client, supportedPackages);
			var neutrals = await CreateNeutrals(client, supportedPackages);
			var henchmen = await CreateHenchmen(client, supportedPackages, abilities);
			var adversaries = await CreateAdversaries(client, supportedPackages, abilities);
			var allies = await CreateAllies(client, supportedPackages, abilities, teams, classes);
			var masterminds = await CreateMasterminds(client, abilities, supportedPackages);
		}

		private static async ValueTask<IReadOnlyList<Team>> CreateTeams(GameServiceClient client)
		{
			ConsoleUtility.WriteLine("Creating teams");
			var teams = new[]
			{
				new Team { Name = "Avengers", ImagePath = "https://qme89g.dm.files.1drv.com/y4mDSjXgLHHwe7E8-y3_Vegi7yhygfTEXtJRkEblsOBVBu_HeB9VKRgOVyXPH9H_HKjAONo5IHf7avPYUab3sRKouqDV1UdxLfR0r05XvAAqJNxUnbySUgeEMgG3hq6zoPJKrz7Bm6J9axLmHvqpqlADlFwV9tM744ByeMZHg4WNxVo6jvPq6YB5jLv8Rd6lknPyJw1v4eZ1JJKr3CRb-nzCg?width=78&height=81&cropmode=none" },
				new Team { Name = "Brotherhood", ImagePath = "https://ocopxg.dm.files.1drv.com/y4mdmtvypD5B_hfq-pgpQTkN3IM4NIGgJySlDHGJ4GWIXy4KcPS2UHqeb0tDYYnfeHYh2RZ1kWztIDYmQ96EqOhGaXKZAs56qphndVwpktGVWI6kbVxxxgt833VEiQM86GtzabZ5rUnnAGmbZmT3ulTuYm9cRY-PejxzjRjVCI9iPsWTiZU02ph-n0hBG1BT5mp2nj_MDdqZkXBxxHo8YvWRQ?width=61&height=84&cropmode=none" },
				new Team { Name = "Cabal", ImagePath = "https://nfeipa.dm.files.1drv.com/y4mjN4qJyVYH3_H98PPbF86DSDfrEKS-ZiiIFC1pVHIpp1FqXqF84fbbwAkb9aibpQc2bU-K1_8FX1V2QHvzmUvQ3-7l8kYFa1oV8UfbeulyjE4bISHpbo0coO46OzyEg61M2hgJmJvdrqgZALL8lRsMt5pZHVL6k6fcPhrlHUrfKoXw_BLP8H3Bhew58KTt-h7Cx24bOZGOySkZbHwu6qNhg?width=84&height=73&cropmode=none" },
				new Team { Name = "Champions", ImagePath = "https://msoc4q.dm.files.1drv.com/y4mNGwtpnGNmNghYtKM89bOLfvElniZSFj_iORcOnPSZXQ-sy5whhQ8JW_yYw1Ru9egPbR_vXfj6J9ETZrMWd_OefGzxaSvjpJgK6iuTSSYGQUSCeI68yhNytjfj1N8jziXy9PCZxvxyCcCgFL7GxAVxQ1xkYBRdHWPzm_9cR3IyKrzg-NjeVBwoGUSYUD39mDE7HQQyPoQSwVJyl-w1s1ZCA?width=84&height=84&cropmode=none" },
				new Team { Name = "Crime Syndicate", ImagePath = "https://ajpagq.dm.files.1drv.com/y4mQmTJwPAXflQRITIA9nO_SWXL_RItBQrSuJvehI1REHKbhYRF2rFx0p_47wL-fUD2GEk-64ApESvNXa6K2kksgZmAgXveQdviXnnZFiuXkbjC1b0iEZYKrGmBkIBlPIlXtPpKwPlqPDci6nccw2JWzD0tT62ljfWl6HPxb4AAonwdu4DAduAJm2EaWC5DWPzazvaEqq1cwWkDUaBxfmFjOw?width=84&height=84&cropmode=none" },
				new Team { Name = "Fantastic Four", ImagePath = "https://ajpxgq.dm.files.1drv.com/y4mAfVOWbPbtiypo7pYsNgdRB2mgt8cG3m5tE-S647CZFpdjwSlc_OmMunKeaCQRypRe_TExPOKpLViV89FJX2JNF4EqzsYMiPjh7DiinYOrRwnRQDayMWj08iGEbKmgVKJbnx0CvECJGl_KFkiiVDSRAMqRfuY8b5DH8BJQfjIjy5I4UqUKE86kldlQGBQl1AJECWNisBGuqVgEDxnJwDSVA?width=84&height=84&cropmode=none" },
				new Team { Name = "Foes of Asgard", ImagePath = "https://z8wy3g.dm.files.1drv.com/y4mdYs7NhlmqCZplwmKpN3K18YERZ2cu3hACSa4MyAncuqXmnB5IhPXHMJGXtXq-JKHkL_rwUPLYEdtHh2H2nKSmMacE95qt4dnJyzAN4WPAn9dV_joFJPfsu75MPaueFkLpv7aWMhQrAdI38PS5Z8pFC4P5AwLXLmaBs1j2MXKFSU6I0iYXfZUkdG6PIhclPJBPUEUWoTTf_xa-z_lc_xhSA?width=61&height=84&cropmode=none" },
				new Team { Name = "Guardians of the Galaxy", ImagePath = "https://ocomxg.dm.files.1drv.com/y4mggnYxWUGUze8-5pSomCm5SQIdVAC_ye7zOerHETz0kuVnQpQjGxqB5IMeqx8H7c1EyS5lBDq7mrY1v0UXfevcGR_Ojn0UTGldPUJvFRDqJnLiA8D_Ccx5wXdvuMk1Ks_j5DeS86tenJM2UuLrXlaheTZJhS3pVwtYke-Onnb0U6o9wfJ_dKFT1gEHVscQIAQIXlK9qXh0qY2VLc0uxDs0w?width=129&height=100&cropmode=none" },
				new Team { Name = "Hydra", ImagePath = "https://mdxjmq.dm.files.1drv.com/y4m7G-rE4Jbb4XWC9eNDiUZ3hRyX_YNq9AoBTd-o3hVSbGrSsNT4nYxMYFpeKkB7UiRBracRUHIOQS-00rmp5aUJXBa7cZHFz6RXDBwgEfmSfaOKOad2VsizDFtAomxqmuPmv0N7Drodn94543l9Z6J-SU-pfGieyS-uxulR14xws7MYXmbhEtsxPx_qy04AlDCXG0b1yEZShwQzvDfJLCxCA?width=84&height=84&cropmode=none" },
				new Team { Name = "Illuminati", ImagePath = "https://nfeipa.dm.files.1drv.com/y4mjN4qJyVYH3_H98PPbF86DSDfrEKS-ZiiIFC1pVHIpp1FqXqF84fbbwAkb9aibpQc2bU-K1_8FX1V2QHvzmUvQ3-7l8kYFa1oV8UfbeulyjE4bISHpbo0coO46OzyEg61M2hgJmJvdrqgZALL8lRsMt5pZHVL6k6fcPhrlHUrfKoXw_BLP8H3Bhew58KTt-h7Cx24bOZGOySkZbHwu6qNhg?width=84&height=73&cropmode=none" },
				new Team { Name = "Marvel Knights", ImagePath = "https://qme59g.dm.files.1drv.com/y4mBOULZqgyAjJ-00_Yg0ZEXHLf5CuOANtpNFYlHf7BACXUCi6Q8e4rd6U3KUsXmbe4xZ6D1moU5xZixyK2zB2BB4qiiAvsnGteqLAdI_HOIOZU53IFyk6Dd9EIN1dbONQ6L4bQE4LpT9t7yhvfI9yDjMNPw6co7dxqhGHJOes8d_nnU6eNGvCrgkPNL0vbcyfcjQ5NNwSJ9ss-7Q90-7CVbg?width=87&height=74&cropmode=none" },
				new Team { Name = "Mercs for Money", ImagePath = "https://baqpiw.dm.files.1drv.com/y4mwwx438Na6jplPpfjcno3GgGUc8lAvB3lLH_Xa_Y1Z2E4peozdcGHYni3AjRx-_A9IlHbyUGeoEsJDUqreg3FTzLc8WsuMD5p62mV_pZ2a8Cw6eGzSwMtYIwA38HMxPs1izM_7m-voGZWKOucmrj08PRRVT829fzIifXdShPJsYVkE0CMfrhhSOYc8GEtNuLGrWcsUQg0uEN7W9QxHX50WQ?width=85&height=85&cropmode=none" },
				new Team { Name = "New Warriors", ImagePath = "https://jsn94q.dm.files.1drv.com/y4m1cB4O26p3BTJoQ5cCaVF61rssrLVWcEKPobLyeTZwBJ4YSCSAEt2iKEe-2ko6RCwkYcyb61pcPuLU-5iKjngFJjgM1izez3XwxULMMno1IKYIfE0zWh7XBB47MhIhgjuQ5DCXq-VqGpNTvi7oJVPLENHtf3ZzgnmbvS9VNFsrkcRcKGysCfy-8itasop6knrU_qaLjLw2tTjujv-cebMnQ?width=67&height=67&cropmode=none" },
				new Team { Name = "None", ImagePath = "https://i07wpg.dm.files.1drv.com/y4mknBclYYxQ0gxh2Ywxjvbs6YG4GoDQbpDy01CIpjwc_-jc2eQRxEhGNwiooMyEHWpEbYlJeJzkOQzdjCt0qhTROfB8B-Cb8qqcaxrx87Ikf33QT-WmiDFqwxpq2roFV73_xLqht4lpVkiRgWJNMOxv4Oc99i8cpNl7A1mQiZSC0LSqmL9fSylB13IKs-oZjDkO7O-sbGmIWvIwAgBLKToQQ?width=84&height=84&cropmode=none" },
				new Team { Name = "Shield", ImagePath = "https://yqmuva.dm.files.1drv.com/y4m1l9oNqHYUPsOpdluLE6TujAXY4E1E7i3z4073M6xmf6h-TrArao_lMRIqsNWUnliCAe31-6JC5NJuFF6aXs4nS4cVie8zWlKZZ3zrcoEn2hUkFylZqwitm_dlF7J3hYMhGEtXeNUKtp_4KvBIzDn-1-Nv13v3eKaEnPtAK1g54MvvZLihGuXiaca3xJlrqg2hs1si3DFUgUBu8MLY7h8HA?width=85&height=85&cropmode=none" },
				new Team { Name = "Sinister Six", ImagePath = "https://i07zpg.dm.files.1drv.com/y4m_CHzznXsk6R8QxDzEo6rkoYBf_Vi0149LBXSy0JyMhdmWCfPSLSnfVaTl8Px7Q1OfgITg8YP_Fl1WuQmfNUfN4LlZ4fMtdVtFTXAD_aD1CxBt-efYaprZ16TqH1jSqyR396-PuEaoSDny8dCnThn9RUSlZbYfYdQrlnaGD7uusfzhe4quyiRUlrXh5DycYe51TfK9cH6LNkM8H9IIqng8g?width=84&height=84&cropmode=none" },
				new Team { Name = "Spider Friends", ImagePath = "https://nfelpa.dm.files.1drv.com/y4m6bwgDQ-YHNIcjfMprCT30BpIAqoct9MAKVYfWmYxgHbkEye6WblxUNQV49bmXcXAL1BKeTGa-NhVm_JuTDoKzmo1YEGmtRJ-utXAItt75n1CNaM_KF_-fdafS6E04SlTXCt5ed14glE2vbMeKgCzTBGfZz9DGvtOusfwAjoKLPwNEXqy-mI1X7OEZlZzClyykxKXbs8jQjEuGCSiB8L2rQ?width=84&height=84&cropmode=none" },
				new Team { Name = "Venomverse", ImagePath = "https://ndxdqq.dm.files.1drv.com/y4mNFr-LW-kvYPIRgdU64SgfvGpXxUtYnc0SARLiMKYK46uFOZ-QVwCNI4h9m0sTT_y6T5cbg53Efi_zEorOkoGnpNNQ7qJ9g23W8HDRTKxj2mKJBefNckjN-6M-X5XPRTvoVbtWZLvoAx8ZBKxv2FpYNcl2LkanETFhDOnWCfIA7iHBgtnOy2EzTgkk7El4jv0rckbDGuRwuBZxS3jMIgZ0A?width=84&height=73&cropmode=none" },
				new Team { Name = "X-Force", ImagePath = "https://yqmrva.dm.files.1drv.com/y4mWTs0Ly5xOvTT2MM2s4vt37HSm1WFSdJaUAYAlK4sqXwcpF_HVe11rXMrrlOaWnPTZGlclPzIWOOxU_qYFKMeXA28l9iico_Myaase_Y232UzW0yz7_bNMRqgasAeyC8DHcLcpNou8HAZNgtB4c0bf3iBV8h_iJvJN8kHvz3RTEfeLMXo_LEvIitZpDz_pl-hgfiO9zD3Q8_CHnVC60jf5g?width=84&height=64&cropmode=none" },
				new Team { Name = "X-Men", ImagePath = "https://i07ypg.dm.files.1drv.com/y4mYFJSdhkGL8AA8qYrTF-iIwOCtHIaXSM96tP3E0kNG6h65_jpOUNvRoED1POlV4euh9FDxntX7b4JF9WiEQz4a4Xd6dKU8blFAepF5Rjgek-6hCzOtqkJb2Uo1Cb627tsOMJXY1uKA66gkR2SN9D3tlFEzfjcnF7lOx7MYvqzgyC_lwjuCUKxKmE2e-b_QqfPJR1o624tNNWqPkJWfSlmEQ?width=84&height=84&cropmode=none" },
			};

			var request = new CreateTeamsRequest();
			request.Teams.AddRange(teams);
			request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

			var reply = await client.CreateTeamsAsync(request);
			if (reply.Status.Code == 200)
				return reply.Teams;

			ConsoleUtility.WriteLine($"Failed to create teams: '{reply.Status.Message}'");
			return await TeamUtility.GetTeamsAsync(client, null);
		}

		private static async ValueTask<IReadOnlyList<Class>> CreateClasses(GameServiceClient client)
		{
			ConsoleUtility.WriteLine("Creating classes");
			var classes = new[]
			{
				new Class { Name = "Covert", ImagePath = "https://ajpzgq.dm.files.1drv.com/y4mXvnkneQVHhJCM2Thk1pl4Cx5baH-wl7L5-6otZ7lOBj6dmZxjWzitVFLHbvRGTWhoq36lDHHFGBf96betGJQ8J5Mjc5agn37dMu4Mih-dgeYZDTYu-eLudNDy-Ntq-U0ryaGtmFeS6vCo0nxA7_yba_6XhsfqpVq_tsKm7QWGIeeUaD6zFy38QRISl5F-og5aOAE5iRFdmlHfJcJOYQJng?width=70&height=71&cropmode=none" },
				new Class { Name = "Strength", ImagePath = "https://nfekpa.dm.files.1drv.com/y4mr6RzK0c1UnrqMTzd2hKAPVazy8uSmlsD_PgJ3TMh4x49A1qmggJEPYzDnXztatHkz-JFdQP6C7E6M-NwJ3cHlu6D8h6Fv-6P2XIlFH1JAqKhPWscJVqO1zVXeyh176wUliaSrVIspBFdsaxVkZQE2synKMSCt-2-yoxZXCKJhU9KP6amVgqOXYE3vWPtOWm74KTHcMwWNXntrzpmsaeqFw?width=73&height=71&cropmode=none" },
				new Class { Name = "Tech", ImagePath = "https://z8wx3g.dm.files.1drv.com/y4mktwH5rgCyBejw6Ib8MMp7-eiZMBEC_xnF3jmW8Jdbt-sqPSoDVIyvmWFNGLbGgENZn9I_mdRPBCF7eB5RPy5hFtl5lELNBrqBkgRUjXH1AnHfyCLlV2XTY_QV_KMqN8wc8_RgSIstZSuY291fLIcL8zisH9rDaewt3NknxopBfqbxD908Cn8HXdl8oG1IIYwQkVhfHW03jD7UlZ-wIhAHg?width=58&height=71&cropmode=none" },
				new Class { Name = "Ranged", ImagePath = "https://z8wx3g.dm.files.1drv.com/y4mktwH5rgCyBejw6Ib8MMp7-eiZMBEC_xnF3jmW8Jdbt-sqPSoDVIyvmWFNGLbGgENZn9I_mdRPBCF7eB5RPy5hFtl5lELNBrqBkgRUjXH1AnHfyCLlV2XTY_QV_KMqN8wc8_RgSIstZSuY291fLIcL8zisH9rDaewt3NknxopBfqbxD908Cn8HXdl8oG1IIYwQkVhfHW03jD7UlZ-wIhAHg?width=58&height=71&cropmode=none" },
				new Class { Name = "Instinct", ImagePath = "https://mdximq.dm.files.1drv.com/y4mT3HV9mjGjRn_aO8yNjtoj_GnsZhZwdXMT6Jt3to6Xsm3TlKphA0iLI6zIjCAm3bBrmubC97eY4HK6p6JL8mZ_XGGk_wZo7gJphFcLNjqyT3TNcRV1rPlu33sEatJIYwWvRL5oAU1rF0RcpMR7G1M7as7VhaFnomuAteXI0ypCDZ3-tG72q-bAnwJu__GeDnc0KVFM03eiT4Yt6GBUq99fg?width=66&height=71&cropmode=none" },
			};

			var request = new CreateClassesRequest();
			request.Classes.AddRange(classes);
			request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

			var reply = await client.CreateClassesAsync(request);
			if (reply.Status.Code == 200)
				return reply.Classes;
			
			ConsoleUtility.WriteLine($"Failed to create classes: '{reply.Status.Message}'");
			return await ClassUtility.GetClassesAsync(client, null);
		}

		private static async ValueTask<IReadOnlyList<Ability>> CreateAbilities(GameServiceClient client, IReadOnlyList<GamePackage> packages)
		{
			ConsoleUtility.WriteLine("Creating abilities");
			var doc = XDocument.Load(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\Abilities\Abilities.xml");

			var request = new CreateAbilitiesRequest();
			request.Abilities.AddRange(doc.Root.Elements("Ability").Select(ability =>
			{
				var gamePackage = packages.First(x => x.Name.Equals(ability.Element("Source").Value, StringComparison.OrdinalIgnoreCase));
				return new Ability { Name = ability.Element("Name").Value, Description = ability.Element("Description").Value, GamePackage = gamePackage };
			}));

			var result = await client.CreateAbilitiesAsync(request);
			ConsoleUtility.WriteLine($"Status: {result.Status.Code}: {result.Status.Message}");

			return result.Abilities;
		}

		private static async ValueTask<IReadOnlyList<Neutral>> CreateNeutrals(GameServiceClient client, IReadOnlyList<GamePackage> packages)
		{
			ConsoleUtility.WriteLine("Creating neutrals");
			List<Neutral> result = new List<Neutral>();
			
			foreach (var file in Directory.EnumerateFiles(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\GameSets", s_fileMask))
			{
				var doc = XDocument.Load(file);

				var name = doc.Element("Set").Attribute("Name").Value;
				var activeGamePackage = packages.FirstOrDefault(x => x.Name == name);
				if (activeGamePackage == null)
					ConsoleUtility.WriteLine($"Failed to find matching game package for {file}");

				foreach (var neutralElement in doc.Element("Set").Element("Cards").Elements("Card").Where(x => x?.Attribute("Area").Value == "Neutral"))
				{
					var request = new CreateNeutralsRequest();
					request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

					var neutral = new Neutral();
					neutral.Name = neutralElement.Attribute("Name").Value;
					neutral.GamePackageId = activeGamePackage.Id;
					
					request.Neutrals.Add(neutral);

					var reply = await client.CreateNeutralsAsync(request);
					if (reply.Status.Code != 200)
						ConsoleUtility.WriteLine($"Failed to create '{neutral.Name}': {reply.Status.Message}");

					result.AddRange(reply.Neutrals);
				}
			}

			return result;
		}

		private static async ValueTask<IReadOnlyList<Henchman>> CreateHenchmen(GameServiceClient client, IReadOnlyList<GamePackage> packages, IReadOnlyList<Ability> abilities)
		{
			ConsoleUtility.WriteLine("Creating henchmen");
			List<Henchman> result = new List<Henchman>();
			
			foreach (var file in Directory.EnumerateFiles(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\GameSets", s_fileMask))
			{
				var doc = XDocument.Load(file);

				var name = doc.Element("Set").Attribute("Name").Value;
				var activeGamePackage = packages.FirstOrDefault(x => x.Name == name);
				if (activeGamePackage == null)
					ConsoleUtility.WriteLine($"Failed to find matching game package for {file}");

				foreach (var henchmanElement in doc.Element("Set").Element("Cards").Elements("Card").Where(x => x?.Attribute("Area").Value == "Henchman"))
				{
					var request = new CreateHenchmenRequest();
					request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

					var henchman = new Henchman();
					henchman.Name = henchmanElement.Attribute("Name").Value;
					henchman.GamePackageId = activeGamePackage.Id;
					henchman.AbilityIds.AddRange(GetMatchingItems(henchmanElement.Attribute("Abilities")?.Value, name => abilities.First(x => x.Name == name)).Select(x => x.Id));
					
					request.Henchmen.Add(henchman);

					var reply = await client.CreateHenchmenAsync(request);
					if (reply.Status.Code != 200)
						ConsoleUtility.WriteLine($"Failed to create '{henchman.Name}': {reply.Status.Message}");

					result.AddRange(reply.Henchmen);
				}

				ConsoleUtility.WriteLine($"Completed: {name}");
			}

			return result;
		}

		private static async ValueTask<IReadOnlyList<Adversary>> CreateAdversaries(GameServiceClient client, IReadOnlyList<GamePackage> packages, IReadOnlyList<Ability> abilities)
		{
			ConsoleUtility.WriteLine("Creating adversaries");
			List<Adversary> result = new List<Adversary>();

			foreach (var file in Directory.EnumerateFiles(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\GameSets", s_fileMask))
			{
				var doc = XDocument.Load(file);

				var name = doc.Element("Set").Attribute("Name").Value;
				var activeGamePackage = packages.FirstOrDefault(x => x.Name == name);
				if (activeGamePackage == null)
					ConsoleUtility.WriteLine($"Failed to find matching game package for {file}");

				foreach (var adversaryElement in doc.Element("Set").Element("Cards").Elements("Card").Where(x => x?.Attribute("Area").Value == "Adversary"))
				{
					var request = new CreateAdversariesRequest();
					request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

					var adversary = new Adversary();
					adversary.Name = adversaryElement.Attribute("Name").Value;
					adversary.GamePackageId = activeGamePackage.Id;
					adversary.AbilityIds.AddRange(GetMatchingItems(adversaryElement.Attribute("Abilities")?.Value, name => abilities.First(x => x.Name == name)).Select(x => x.Id));

					request.Adversaries.Add(adversary);

					var reply = await client.CreateAdversariesAsync(request);
					if (reply.Status.Code != 200)
						ConsoleUtility.WriteLine($"Failed to create '{adversary.Name}': {reply.Status.Message}");

					result.AddRange(reply.Adversaries);
				}

				ConsoleUtility.WriteLine($"Completed: {name}");
			}

			return result;
		}

		private static async ValueTask<IReadOnlyList<Ally>> CreateAllies(GameServiceClient client, IReadOnlyList<GamePackage> packages, IReadOnlyList<Ability> abilities, IReadOnlyList<Team> teams, IReadOnlyList<Class> classes)
		{
			ConsoleUtility.WriteLine("Creating allies");
			List<Ally> result = new List<Ally>();

			var noTeam = teams.First(x => x.Name == "None");

			foreach (var file in Directory.EnumerateFiles(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\GameSets", s_fileMask))
			{
				var doc = XDocument.Load(file);

				var name = doc.Element("Set").Attribute("Name").Value;
				var activeGamePackage = packages.FirstOrDefault(x => x.Name == name);
				if (activeGamePackage == null)
					ConsoleUtility.WriteLine($"Failed to find matching game package for {file}");

				foreach (var allyElement in doc.Element("Set").Element("Cards").Elements("Card").Where(x => x?.Attribute("Area").Value == "Ally"))
				{
					var request = new CreateAlliesRequest();
					request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

					var ally = new Ally();
					ally.Name = allyElement.Attribute("Name").Value;
					ally.GamePackageId = activeGamePackage.Id;
					ally.AbilityIds.AddRange(GetMatchingItems(allyElement.Attribute("Abilities")?.Value, name => abilities.First(x => x.Name == name)).Select(x => x.Id));
					ally.TeamId = GetMatchingItems(allyElement.Attribute("Team")?.Value, name => teams.FirstOrDefault(x => x.Name == name, noTeam)).First().Id;
					ally.Classes.AddRange(GetMatchingItems(allyElement.Attribute("Classes")?.Value, name => ParseClassInfo(classes, name)));

					request.Allies.Add(ally);

					var reply = await client.CreateAlliesAsync(request);
					if (reply.Status.Code != 200)
						ConsoleUtility.WriteLine($"Failed to create '{ally.Name}': {reply.Status.Message}");
					
					result.AddRange(reply.Allies);
				}

				ConsoleUtility.WriteLine($"Completed: {name}");
			}

			return result;
		}

		private static async ValueTask<IReadOnlyList<Mastermind>> CreateMasterminds(GameServiceClient client, IReadOnlyList<Ability> abilities, IReadOnlyList<GamePackage> packages)
		{
			ConsoleUtility.WriteLine("Creating masterminds");
			List<Mastermind> result = new List<Mastermind>();

			foreach (var file in Directory.EnumerateFiles(@"C:\Users\Ryan\SkyDrive\code\LegendaryGameStarter\LegendaryGameModel2\GameSets", s_fileMask))
			{
				var doc = XDocument.Load(file);

				var name = doc.Element("Set").Attribute("Name").Value;
				var activeGamePackage = packages.FirstOrDefault(x => x.Name == name);
				if (activeGamePackage == null)
					ConsoleUtility.WriteLine($"Failed to find matching game package for {file}");

				foreach (var mastermindElement in doc.Element("Set").Element("Cards").Elements("Card").Where(x => x?.Attribute("Area").Value == "Mastermind"))
				{
					var request = new CreateMastermindsRequest();
					request.CreateOptions.Add(CreateOptions.ErrorOnDuplicates);

					var mastermind = new Mastermind();
					mastermind.Name = mastermindElement.Attribute("Name").Value;
					mastermind.GamePackageId = activeGamePackage.Id;
					mastermind.AbilityIds.AddRange(GetMatchingItems(mastermindElement.Attribute("Abilities")?.Value, name => abilities.First(x => x.Name == name)).Select(x => x.Id));
					mastermind.CardRequirements.AddRange(GetCardRequirements(client, mastermindElement, activeGamePackage.Id));
					mastermind.HasEpicSide = (mastermindElement.Attribute("HasEpicSide")?.Value.ToLower() ?? "false") == "true";
					ConsoleUtility.WriteLine(mastermind.ToString());

					request.Masterminds.Add(mastermind);

					var reply = await client.CreateMastermindsAsync(request);
					if (reply.Status.Code != 200)
						ConsoleUtility.WriteLine($"Failed to create '{mastermind.Name}': {reply.Status.Message}");

					result.AddRange(reply.Masterminds);
				}

				ConsoleUtility.WriteLine($"Completed: {name}");
			}

			return result;
		}

		private static IEnumerable<CardRequirement> GetCardRequirements(GameServiceClient client, XElement element, int gamePackageId)
		{
			foreach (var attribute in element.Attributes())
			{
				var cardRequirements = attribute.Name.LocalName switch
				{
					"Name" => null,
					"Area" => null,
					"Abilities" => null,
					"HasEpicSide" => null,
					"RequiresAllOf" => attribute.Value.Split('|').Select(x => GetCardSetRequirement(client, x, gamePackageId)),
					"RequiresHenchmenNamed" => GetCardGroupRequirement(client, attribute.Value, gamePackageId),
					_ => throw new Exception($"Don't know how to handle {attribute.Name}")
				};

				foreach (var cardRequirement in cardRequirements ?? new CardRequirement[] { } )
					yield return cardRequirement;
			}
		}

		private static CardRequirement GetCardSetRequirement(GameServiceClient client, string cardName, int gamePackageId)
		{
			var adversaries = AdversaryUtility.GetAdversariesAsync(client, name: cardName, nameMatchStyle: NameMatchStyle.Exact).Result;
			if (adversaries.Count() != 0)
			{
				return new CardRequirement
				{
					RequiredSetId = adversaries.FirstOrDefault(x => x.GamePackageId == gamePackageId, adversaries.First()).Id,
					CardSetType = CardSetType.CardSetAdversary
				};
			}

			var henchmen = HenchmanUtility.GetHenchmenAsync(client, name: cardName, nameMatchStyle: NameMatchStyle.Exact).Result;
			if (henchmen.Count() != 0)
			{
				return new CardRequirement
				{
					RequiredSetId = henchmen.FirstOrDefault(x => x.GamePackageId == gamePackageId, henchmen.First()).Id,
					CardSetType = CardSetType.CardSetHenchman
				};
			}

			var neutrals = NeutralUtility.GetNeutralsAsync(client, name: cardName, nameMatchStyle: NameMatchStyle.Exact).Result;
			if (neutrals.Count() != 0)
			{
				return new CardRequirement
				{
					RequiredSetId = neutrals.FirstOrDefault(x => x.GamePackageId == gamePackageId, neutrals.First()).Id,
					CardSetType = CardSetType.CardSetNeutral
				};
			}

			throw new Exception($"Coudn't figure out how to handle {cardName}");
		}

		private static IEnumerable<CardRequirement> GetCardGroupRequirement(GameServiceClient client, string cardNameMatch, int gamePackageId)
		{
			yield return new CardRequirement
			{
				RequiredSetName = cardNameMatch,
				CardSetType = CardSetType.CardSetHenchman
			};
		}

		private static ClassInfo ParseClassInfo(IReadOnlyList<Class> classes, string data)
		{
			var splitData = data.Split('/');
			 var count = int.Parse(splitData[1]);
			return new ClassInfo { ClassId = classes.First(x => x.Name == splitData[0]).Id, Count = count };
		}

		private static IReadOnlyList<T> GetMatchingItems<T>(string input, Func<string, T> lookup)
		{
			List<T> foundItems = new List<T>();

			if (!string.IsNullOrWhiteSpace(input))
			{
				foreach (var item in input.Split('|'))
				{
					var foundItem = lookup(item);
					if (foundItem == null)
						throw new Exception($"Couldn't find item '{item}'");

					foundItems.Add(foundItem);
				}
			}

			return foundItems;
		}

		private static async ValueTask CreateGamePackages(GameServiceClient client)
		{
			var sets = new[]
			{
				new GamePackage { Name = "Legendary", PackageType = GamePackageType.BaseGame, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Legendary Marvel Studios", PackageType = GamePackageType.BaseGame, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Legendary Villains", PackageType = GamePackageType.BaseGame, BaseMap = GameBaseMap.Villains },

				new GamePackage { Name = "X-Men", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "World War Hulk", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Secret Wars Volume 1", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Secret Wars Volume 2", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Civil War", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Revelations", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Dark City", PackageType = GamePackageType.LargeExpansion, BaseMap = GameBaseMap.Legendary },

				new GamePackage { Name = "Venom", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Spider-Man Homecoming", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Paint the Town Red (Spider-Man)", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Noir", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Guardians of the Galaxy", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Fantastic Four", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Deadpool", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Champions", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Captain America 75th Anniversary", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Ant-Man", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Dimensions", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Legendary },
				new GamePackage { Name = "Fear Itself", PackageType = GamePackageType.SmallExpansion, BaseMap = GameBaseMap.Villains },
			};

			foreach (var set in sets)
			{
				var createRequest = new CreateGamePackageRequest
				{
					Name = set.Name,
					PackageType = set.PackageType,
					BaseMap = set.BaseMap
				};

				var createResponse = await client.CreateGamePackageAsync(createRequest);

				ConsoleUtility.WriteLine($"{set.Name} - {createResponse.Id} : {createResponse.Status?.Code}");
			}
		}

		private readonly static string s_fileMask = "*.xml";
	}
}