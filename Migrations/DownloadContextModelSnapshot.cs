﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShareLoader.Data;

namespace ShareLoader.Migrations
{
    [DbContext(typeof(DownloadContext))]
    partial class DownloadContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("ShareLoader.Models.AccountModel", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AllowClientRedirect")
                        .HasColumnType("INTEGER");

                    b.Property<float>("Credit")
                        .HasColumnType("REAL");

                    b.Property<string>("Hoster")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsPremium")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(20)
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<float>("TrafficLeft")
                        .HasColumnType("REAL");

                    b.Property<float>("TrafficLeftWeek")
                        .HasColumnType("REAL");

                    b.Property<string>("Username")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ValidTill")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("ShareLoader.Models.AppHash", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Secret")
                        .HasMaxLength(16)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Codes");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadError", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Error")
                        .HasColumnType("TEXT");

                    b.Property<int>("ItemID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Message")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<int>("SourceId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Errors");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadGroup", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsTemp")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Sort")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("ShareLoader.Models.DownloadItem", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("DownloadGroupID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GroupID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Hoster")
                        .HasMaxLength(3)
                        .HasColumnType("TEXT");

                    b.Property<string>("MD5")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("ShareId")
                        .HasMaxLength(15)
                        .HasColumnType("TEXT");

                    b.Property<int>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Url")
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("ShareLoader.Models.StatisticModel", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("EntityID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EntityType")
                        .HasMaxLength(25)
                        .HasColumnType("TEXT");

                    b.Property<int>("Source")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Stamp")
                        .HasColumnType("TEXT");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("ID");

                    b.ToTable("Statistics");
                });
#pragma warning restore 612, 618
        }
    }
}