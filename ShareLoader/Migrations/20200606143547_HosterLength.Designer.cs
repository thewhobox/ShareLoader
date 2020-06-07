﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShareLoader.Data;

namespace ShareLoader.Migrations
{
    [DbContext(typeof(DownloadContext))]
    [Migration("20200606143547_HosterLength")]
    partial class HosterLength
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ShareLoader.Models.AccountModel", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<float>("Credit");

                    b.Property<string>("Hoster")
                        .HasMaxLength(2);

                    b.Property<bool>("IsPremium");

                    b.Property<string>("Name")
                        .HasMaxLength(20);

                    b.Property<string>("Password")
                        .HasMaxLength(50);

                    b.Property<float>("TrafficLeft");

                    b.Property<float>("TrafficLeftWeek");

                    b.Property<string>("Username")
                        .HasMaxLength(50);

                    b.Property<DateTime>("ValidTill");

                    b.HasKey("ID");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("ShareLoader.Models.AppHash", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Secret")
                        .HasMaxLength(16);

                    b.HasKey("Id");

                    b.ToTable("Codes");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadError", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Error");

                    b.Property<int>("ItemID");

                    b.Property<string>("Message")
                        .HasMaxLength(200);

                    b.Property<int>("SourceId");

                    b.Property<DateTime>("Time");

                    b.HasKey("ID");

                    b.ToTable("Errors");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadGroup", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("IsTemp");

                    b.Property<string>("Name")
                        .HasMaxLength(200);

                    b.Property<string>("Password")
                        .HasMaxLength(100);

                    b.Property<string>("Sort");

                    b.Property<int>("Target");

                    b.Property<int>("Type");

                    b.HasKey("ID");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadItem", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DownloadGroupID");

                    b.Property<int>("GroupID");

                    b.Property<string>("Hoster")
                        .HasMaxLength(3);

                    b.Property<string>("MD5")
                        .HasMaxLength(32);

                    b.Property<string>("Name")
                        .HasMaxLength(200);

                    b.Property<string>("ShareId")
                        .HasMaxLength(15);

                    b.Property<int>("Size");

                    b.Property<int>("State");

                    b.Property<string>("Url")
                        .HasMaxLength(150);

                    b.HasKey("ID");

                    b.HasIndex("DownloadGroupID");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("ShareLoader.Models.StatisticModel", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("EntityID");

                    b.Property<string>("EntityType")
                        .HasMaxLength(25);

                    b.Property<int>("Source");

                    b.Property<DateTime>("Stamp");

                    b.Property<double>("Value");

                    b.HasKey("ID");

                    b.ToTable("Statistics");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadItem", b =>
                {
                    b.HasOne("ShareLoader.Models.DownloadGroup")
                        .WithMany("Items")
                        .HasForeignKey("DownloadGroupID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
