using KeepersCompound.Formats.TagFile.Blocks.GamFile;
using KeepersCompound.Formats.TagFile.Blocks.LmParams;
using KeepersCompound.Formats.TagFile.Blocks.Props.Bool;
using KeepersCompound.Formats.TagFile.Blocks.Props.Door.RotDoor;
using KeepersCompound.Formats.TagFile.Blocks.Props.Door.TransDoor;
using KeepersCompound.Formats.TagFile.Blocks.Props.Position;
using KeepersCompound.Formats.TagFile.Blocks.Props.RenderType;
using KeepersCompound.Formats.TagFile.Blocks.RendParams;
using KeepersCompound.Formats.TagFile.Blocks.TxList;
using KeepersCompound.Formats.TagFile.Blocks.Unknown;

namespace KeepersCompound.Formats.TagFile.Blocks;

public static class BlockParserMapper
{
    public static IBinaryParser<AbstractBlock> GetBlockParser(TocEntry entry)
    {
        return entry.Tag switch
        {
            "GAM_FILE" => new GamFileBlockParser(),
            "LM_PARAM" => new LmParamsBlockParser(),
            "P$AIFiresTh" => new BoolBlockParser<AiFiresThroughProp>(entry),
            "P$AI_BlkVis" => new BoolBlockParser<BlocksAiVisionProp>(entry),
            "P$AI_Fidget" => new BoolBlockParser<AiFidgetProp>(entry),
            "P$AI_FleeAw" => new BoolBlockParser<AiFleeAwareProp>(entry),
            "P$AI_IdlRet" => new BoolBlockParser<AiReturnOriginProp>(entry),
            "P$AI_IgCam" => new BoolBlockParser<AiIgnoresCamerasProp>(entry),
            "P$AI_InfFrm" => new BoolBlockParser<AiInformFromProp>(entry),
            "P$AI_InfNow" => new BoolBlockParser<AiImmediateInformProp>(entry),
            "P$AI_InfOtr" => new BoolBlockParser<AiInformOthersProp>(entry),
            "P$AI_IsBig" => new BoolBlockParser<AiNeedsBigDoorsProp>(entry),
            "P$AI_IsProx" => new BoolBlockParser<AiIsProxyProp>(entry),
            "P$AI_IsSmal" => new BoolBlockParser<AiIsSmallProp>(entry),
            "P$AI_Launch" => new BoolBlockParser<AiLaunchVisProp>(entry),
            "P$AI_NCDmRs" => new BoolBlockParser<AiRespondToDamageProp>(entry),
            "P$AI_NGOBB" => new BoolBlockParser<AiPathExactObbProp>(entry),
            "P$AI_NoGhos" => new BoolBlockParser<AiNoMultiplayerGhostProp>(entry),
            "P$AI_NoHand" => new BoolBlockParser<AiNoMultiplayerHandoffProp>(entry),
            "P$AI_Notice" => new BoolBlockParser<AiNoticesDamageProp>(entry),
            "P$AI_NtcBod" => new BoolBlockParser<AiNoticesBodiesProp>(entry),
            "P$AI_ObjPat" => new BoolBlockParser<AiPathableObjectProp>(entry),
            "P$AI_OnlyPl" => new BoolBlockParser<AiOnlyNoticesPlayerProp>(entry),
            "P$AI_Patrol" => new BoolBlockParser<AiDoesPatrolProp>(entry),
            "P$AI_PtrlRn" => new BoolBlockParser<AiRandomPatrolProp>(entry),
            "P$AI_SaveCo" => new BoolBlockParser<AiSaveConversationProp>(entry),
            "P$AI_SeesPr" => new BoolBlockParser<AiSeesProjectilesProp>(entry),
            "P$AI_TrackM" => new BoolBlockParser<AiTrackMediumProp>(entry),
            "P$AI_UseWat" => new BoolBlockParser<AiPathWaterProp>(entry),
            "P$AI_UsesDo" => new BoolBlockParser<AiUsesDoorsProp>(entry),
            "P$BlockFrob" => new BoolBlockParser<BlockFrobProp>(entry),
            "P$Blood" => new BoolBlockParser<BloodProp>(entry),
            "P$BloodCaus" => new BoolBlockParser<BloodCauseProp>(entry),
            "P$Borrowed" => new BoolBlockParser<BorrowedProp>(entry),
            "P$Borrowing" => new BoolBlockParser<BorrowingProp>(entry),
            "P$Bump Map" => new BoolBlockParser<BumpMapProp>(entry),
            "P$ContainIn" => new BoolBlockParser<ContainInheritProp>(entry),
            "P$CretHTrac" => new BoolBlockParser<DisableHeadTrackingProp>(entry),
            "P$Culpable" => new BoolBlockParser<CulpableProp>(entry),
            "P$DistinctA" => new BoolBlockParser<DistinctAvatarProp>(entry),
            "P$DoorStati" => new BoolBlockParser<DoorStaticLightProp>(entry),
            "P$Face Pos" => new BoolBlockParser<FaceMotionsProp>(entry),
            "P$Fixture" => new BoolBlockParser<FixtureProp>(entry),
            "P$FromBrief" => new BoolBlockParser<FromBriefcaseProp>(entry),
            "P$Fungus" => new BoolBlockParser<FungusProp>(entry),
            "P$HTHGruntA" => new BoolBlockParser<AiMeleeGruntAlwaysProp>(entry),
            "P$HasBrush" => new BoolBlockParser<HasBrushProp>(entry),
            "P$HasRefs" => new BoolBlockParser<HasRefsProp>(entry),
            "P$Immobile" => new BoolBlockParser<ImmobileProp>(entry),
            "P$InvBeingT" => new BoolBlockParser<InvBeingTakenProp>(entry),
            "P$ItemStore" => new BoolBlockParser<ItemStoreProp>(entry),
            "P$LocalCopy" => new BoolBlockParser<LocalCopyProp>(entry),
            "P$Locked" => new BoolBlockParser<LockedProp>(entry),
            "P$NoBlockCo" => new BoolBlockParser<NeverBlockCoronasProp>(entry),
            "P$NoBorrow" => new BoolBlockParser<NoBorrowProp>(entry),
            "P$NoDrop" => new BoolBlockParser<InvNoDropProp>(entry),
            "P$NoFlash" => new BoolBlockParser<FlashInvulnerableProp>(entry),
            "P$NonPhysCr" => new BoolBlockParser<NonPhysCreatureProp>(entry),
            "P$ObjShad" => new BoolBlockParser<RuntimeObjectShadowProp>(entry),
            "P$PhysAICol" => new BoolBlockParser<AiCollidesWithProp>(entry),
            "P$PhysCanMa" => new BoolBlockParser<MantleableProp>(entry),
            "P$PhysFaceV" => new BoolBlockParser<FacesVelocityProp>(entry),
            "P$Position" => new PositionBlockParser(entry),
            "P$Preload" => new BoolBlockParser<PreloadProp>(entry),
            "P$RenderTyp" => new RenderTypeBlockParser(entry),
            "P$RngdGrunt" => new BoolBlockParser<AiRangedGruntAlwaysProp>(entry),
            "P$RotDoor" => new RotDoorBlockParser(entry),
            "P$StatShad" => new BoolBlockParser<ForceStaticShadowProp>(entry),
            "P$StimKO" => new BoolBlockParser<AiIsKnockoutProp>(entry),
            "P$TransDoor" => new TransDoorBlockParser(entry),
            "P$Transient" => new BoolBlockParser<TransientProp>(entry),
            "P$WpnTerrCo" => new BoolBlockParser<WpnTerrCollProp>(entry),
            "RENDPARAMS" => new RendParamsBlockParser(),
            "TXLIST" => new TxListBlockParser(),
            _ => new UnknownBlockParser(entry),
        };
    }
}