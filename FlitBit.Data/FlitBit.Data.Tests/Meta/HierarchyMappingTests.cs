﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlitBit.Data.Meta;
using FlitBit.Data.Tests.Meta.Models;
using FlitBit.Data.SqlServer;
using FlitBit.Emit;

namespace FlitBit.Data.Tests.Meta
{
	[TestClass]
	public class HierarchyMappingTests
	{
		[TestInitialize]
		public void Initialize()
		{
			// force the dynamic assembly to disk so we can put eyeballs on the code...
			RuntimeAssemblies.WriteDynamicAssemblyOnExit = true;
		}

		[TestMethod]
		public void TestMethod1()
		{
			var party = Mappings.Instance.ForType<IParty>();
			Assert.IsNotNull(party);
			Assert.IsNotNull(party.Columns);
			Assert.AreEqual(5, party.Columns.Count());

			var binder = party.GetBinder();
			var builder = new StringBuilder(2000);			
			binder.BuildDdlBatch(builder);
			var sql = builder.ToString();
			
			Assert.IsNotNull(sql);

			var people = Mappings.Instance.ForType<IPerson>();
			Assert.IsNotNull(people);

			Assert.IsNotNull(people.Columns);

			// IParty: 5, IPerson: 4, IEmailAddress: 2
			Assert.AreEqual(11, people.Columns.Count());

			var peepsBinder = people.GetBinder();
			
			builder = new StringBuilder(2000);
			peepsBinder.BuildDdlBatch(builder);
			sql = builder.ToString();

			Assert.IsNotNull(sql);

		}
	}
}
