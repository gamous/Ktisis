﻿using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Localization;
using Ktisis.Structs.Actor;
using static Ktisis.Overlay.Skeleton;

namespace Ktisis.Structs.Bones {
	public class Bone {
		public int Index;
		public int Partial;
		public unsafe hkaPose* Pose;
		public unsafe Skeleton* Skeleton;

		public unsafe Bone(Skeleton* skeleton, int partialId, int boneId) {
			Index = boneId;
			Partial = partialId;

			var partial = skeleton->PartialSkeletons[partialId];
			var pose = partial.GetHavokPose(0);
			Pose = pose;
			Skeleton = skeleton;
		}

		public unsafe hkaBone HkaBone => Pose->Skeleton->Bones[Index];
		public unsafe int ParentId => Pose->Skeleton->ParentIndices[Index];
		public unsafe hkQsTransformf Transform {
			get => Pose->ModelPose.Data[Index];
			set => Pose->ModelPose.Data[Index] = value;
		}

		public string LocaleName => Locale.GetBoneName(HkaBone.Name.String);

		public string UniqueId => $"{Partial}_{Index}";
		public string UniqueName => $"{LocaleName}##{UniqueId}";

		public List<Category> Categories => Category.GetForBone(HkaBone.Name.String);

		public unsafe hkQsTransformf* AccessModelSpace(PropagateOrNot propagate) => Pose->AccessBoneModelSpace(Index, propagate);

		public unsafe Vector3 GetWorldPos(ActorModel* model) => model->Position + GetOffset(model) + Transform.Translation.Rotate(model->Rotation) * model->Height * model->Scale;
		private unsafe Vector3 GetOffset(ActorModel* model) => CustomOffset.CalculateWorldOffset(model, this);
		public unsafe List<Bone> GetChildren() {
			var result = new List<Bone>();
			// Add child bones from same partial
			for (var i = Index + 1; i < Pose->Skeleton->ParentIndices.Length; i++) {
				var child = new Bone(Skeleton, Partial, i);
				if (child.ParentId != Index) continue;
				result.Add(child);
			}
			// Add child bones from connected partials
			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				if (p == Partial) continue;
				var partial = Skeleton->PartialSkeletons[p];
				if (partial.ConnectedParentBoneIndex == Index) {
					var partialRoot = new Bone(Skeleton, p, partial.ConnectedBoneIndex);
					var children = partialRoot.GetChildren();
					foreach (var child in children)
						result.Add(child);
				}
			}
			return result;
		}

		public unsafe Bone? GetMirrorSibling() {
			var name = HkaBone.Name.String;
			var prefix = name[..^2];

			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				var partial = Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var poseSkeleton = pose->Skeleton;
				for (var i = 1; i < poseSkeleton->Bones.Length; i++) {
					var potentialBone = new Bone(Skeleton, p, i);
					if (potentialBone == null) continue;
					var pBName = potentialBone.HkaBone.Name.String;
					if (pBName[..^2] == prefix && pBName != name)
						return potentialBone;
				}
			}
			return null;
		}

		public List<Bone> GetDescendants() {
			var list = GetChildren();
			for (var i = 0; i < list.Count; i++)
				list.AddRange(list[i].GetChildren());
			return list;
		}

		public unsafe void PropagateChildren(hkQsTransformf* transform, Vector3 initialPos, Quaternion initialRot) {
			// Bone parenting
			// Adapted from Anamnesis Studio code shared by Yuki - thank you!

			if (!Ktisis.Configuration.EnableParenting)
				return;

			var sourcePos = transform->Translation.ToVector3();
			var deltaRot = transform->Rotation.ToQuat() / initialRot;
			var deltaPos = sourcePos - initialPos;

			var descendants = GetDescendants();
			foreach (var child in descendants) {
				var access = child.AccessModelSpace(PropagateOrNot.DontPropagate);

				var offset = access->Translation.ToVector3() - sourcePos;
				offset = Vector3.Transform(offset, deltaRot);

				var matrix = Interop.Alloc.GetMatrix(access);
				matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
				matrix.Translation = deltaPos + sourcePos + offset;
				Interop.Alloc.SetMatrix(access, matrix);
			}
		}

		public unsafe void PropagateSibling(Quaternion deltaRot) {
			if (Ktisis.Configuration.SiblingLink == SiblingLink.None) return;

			var access = AccessModelSpace(PropagateOrNot.DontPropagate);
			var offset = access->Translation.ToVector3();

			if (Ktisis.Configuration.SiblingLink == SiblingLink.RotationMirrorX)
				deltaRot = new(-deltaRot.X, deltaRot.Y, deltaRot.Z, -deltaRot.W);

			var matrix = Interop.Alloc.GetMatrix(access);
			matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = offset;

			var initialRot = access->Rotation.ToQuat();
			var initialPos = access->Translation.ToVector3();
			Interop.Alloc.SetMatrix(access, matrix);

			this.PropagateChildren(access, initialPos, initialRot);
		}

		public bool IsBusted() =>
			float.IsNaN(Transform.Translation.X)
			|| float.IsNaN(Transform.Translation.Y)
			|| float.IsNaN(Transform.Translation.Z)
			|| Transform.Rotation.W == 0;
	}
}