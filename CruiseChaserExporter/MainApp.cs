using CruiseChaserExporter.ComposedModel;
using CruiseChaserExporter.Gltf;
using CruiseChaserExporter.XivStruct;
using Lumina;

namespace CruiseChaserExporter;

public static class MainApp {
    public static ModelExporter ExporterFromMonster(GameData lumina, int modelId, int bodyId, int variantId,
        IEnumerable<string>? animations = null) {
        var exporter = new ComposedModel.ModelExporter(lumina);
        exporter.AddMonster(modelId, bodyId, variantId);
        foreach (var path in animations ?? Array.Empty<string>())
            exporter.AddAnimation(path);
        return exporter;
    }

    public static void Main() {
        var lumina = new GameData(
            @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack");

        switch (6) {
            case 0: // Random chair
                new ModelExporter(lumina)
                    .AddModel("bg/ffxiv/sea_s1/twn/s1ta/bgparts/s1ta_ga_char1.mdl")
                    .Export()
                    .Save("chair");
                break;
            case 1: // Cruise Chaser (A11)
                ExporterFromMonster(lumina, 361, 1, 1, Resources.m0361.Split("\n")).Export().Save("m0361_b0001_v0001");
                break;
            case 2: // Mustadio (Orbonne)
                ExporterFromMonster(lumina, 361, 3, 1, Resources.m0361.Split("\n")).Export().Save("m0361_b0003_v0001");
                break;
            case 3: // Construct 14 (Ridorana)
                ExporterFromMonster(lumina, 489, 1, 1, Resources.m0489.Split("\n")).Export().Save("m0489_b0001_v0001");
                break;
            case 4: // Grebuloff
                // TODO: Make alpha work
                new ModelExporter(lumina)
                    .AddMonster(405, 2, 1)
                    .Export()
                    .Save("m0450_b0002_v0001");
                break;
            case 5: // Default human
                new ModelExporter(lumina, XivHumanSkeletonId.HyurMidlanderMale)
                    .AddModel("chara/human/c0101/obj/face/f0002/model/c0101f0002_fac.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_top.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_glv.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_dwn.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_sho.mdl")
                    // 32-bit
                    .AddAnimation("chara/human/c0101/animation/a0001/bt_common/battle/auto_attack3.pap")
                    // .AddAnimation("chara/human/c0101/animation/a0010/bt_common/idle_sp/idle_sp_1.pap")
                    // 48-bit
                    // .AddAnimation("chara/human/c0101/animation/a0137/bt_common/event_base/event_base_idle1.pap")
                    .Export()
                    .Save("DefaultHuman");
                break;
            case 6: // Default Roe
                new ModelExporter(lumina, XivHumanSkeletonId.RoegadynMale)
                    .AddModel("chara/human/c0901/obj/face/f0002/model/c0901f0002_fac.mdl")
                    .AddModel("chara/human/c0901/obj/hair/h0007/model/c0901h0007_hir.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_top.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_glv.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_dwn.mdl")
                    .AddModel("chara/human/c0101/obj/body/b0001/model/c0101b0001_sho.mdl")
                    // 32-bit
                    // .AddAnimation("chara/human/c0101/animation/a0001/bt_common/battle/auto_attack3.pap")
                    // .AddAnimation("chara/human/c0101/animation/a0010/bt_common/idle_sp/idle_sp_1.pap")
                    // 48-bit
                    // .AddAnimation("chara/human/c0101/animation/a0137/bt_common/event_base/event_base_idle1.pap")
                    .Export()
                    .Save("DefaultHuman");
                break;
            default:
                return;
        }
    }
}
