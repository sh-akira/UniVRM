﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UniJSON;
using UnityEngine;


namespace UniGLTF
{
    public class UniGLTFTests
    {
        static GameObject CreateSimpleScene()
        {
            var root = new GameObject("gltfRoot").transform;

            var scene = new GameObject("scene0").transform;
            scene.SetParent(root, false);
            scene.localPosition = new Vector3(1, 2, 3);

            return root.gameObject;
        }

        void AssertAreEqual(Transform go, Transform other)
        {
            var lt = go.Traverse().GetEnumerator();
            var rt = go.Traverse().GetEnumerator();

            while (lt.MoveNext())
            {
                if (!rt.MoveNext())
                {
                    throw new Exception("rt shorter");
                }

                MonoBehaviourComparator.AssertAreEquals(lt.Current.gameObject, rt.Current.gameObject);
            }

            if (rt.MoveNext())
            {
                throw new Exception("rt longer");
            }
        }

        [Test]
        public void UniGLTFSimpleSceneTest()
        {
            var go = CreateSimpleScene();
            var context = new ImporterContext();

            try
            {
                // export
                var gltf = new glTF();

                string json = null;
                using (var exporter = new gltfExporter(gltf))
                {
                    exporter.Prepare(go);
                    exporter.Export();

                    // remove empty buffer
                    gltf.buffers.Clear();

                    json = gltf.ToJson();
                }

                // import
                context.ParseJson(json, new SimpleStorage(new ArraySegment<byte>()));
                //Debug.LogFormat("{0}", context.Json);
                context.Load();

                AssertAreEqual(go.transform, context.Root.transform);
            }
            finally
            {
                //Debug.LogFormat("Destory, {0}", go.name);
                GameObject.DestroyImmediate(go);
                context.EditorDestroyRootAndAssets();
            }
        }

        void BufferTest(int init, params int[] size)
        {
            var initBytes = init == 0 ? null : new byte[init];
            var storage = new ArrayByteBuffer(initBytes);
            var buffer = new glTFBuffer(storage);

            var values = new List<byte>();
            int offset = 0;
            foreach (var x in size)
            {
                var nums = Enumerable.Range(offset, x).Select(y => (Byte)y).ToArray();
                values.AddRange(nums);
                var bytes = new ArraySegment<Byte>(nums);
                offset += x;
                buffer.Append(bytes, glBufferTarget.NONE);
            }

            Assert.AreEqual(values.Count, buffer.byteLength);
            Assert.True(Enumerable.SequenceEqual(values, buffer.GetBytes().ToArray()));
        }

        [Test]
        public void BufferTest()
        {
            BufferTest(0, 0, 100, 200);
            BufferTest(0, 128);
            BufferTest(0, 256);

            BufferTest(1024, 0);
            BufferTest(1024, 128);
            BufferTest(1024, 2048);
            BufferTest(1024, 900, 900);
        }

        [Test]
        public void UnityPathTest()
        {
            var root = UnityPath.FromUnityPath(".");
            Assert.IsFalse(root.IsNull);
            Assert.IsFalse(root.IsUnderAssetsFolder);
            Assert.AreEqual(UnityPath.FromUnityPath("."), root);

            var assets = UnityPath.FromUnityPath("Assets");
            Assert.IsFalse(assets.IsNull);
            Assert.IsTrue(assets.IsUnderAssetsFolder);

            var rootChild = root.Child("Assets");
            Assert.AreEqual(assets, rootChild);

            var assetsChild = assets.Child("Hoge");
            var hoge = UnityPath.FromUnityPath("Assets/Hoge");
            Assert.AreEqual(assetsChild, hoge);

            //var children = root.TravserseDir().ToArray();
        }

        [Test]
        public void VersionChecker()
        {
            Assert.False(ImporterContext.IsGeneratedUniGLTFAndOlderThan("hoge", 1, 16));
            Assert.False(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-1.16", 1, 16));
            Assert.True(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-1.15", 1, 16));
            Assert.False(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-11.16", 1, 16));
            Assert.True(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-0.16", 1, 16));
            Assert.True(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF", 1, 16));
        }

        [Test]
        public void MeshTest()
        {
            var mesh = new glTFMesh("mesh")
            {
                primitives = new List<glTFPrimitives>
                {
                    new glTFPrimitives
                    {
                        attributes = new glTFAttributes
                        {
                            POSITION = 0,
                        }
                    }
                },
            };

            var f = new JsonFormatter();
            f.Serialize(mesh);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""name"":""mesh"",""primitives"":[{""mode"":0,""attributes"":{""POSITION"":0},""material"":0,""extensions"":{}}]}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFMesh>().Serialize(mesh, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void PrimitiveTest()
        {
            var prim = new glTFPrimitives
            {
                attributes = new glTFAttributes
                {
                    POSITION = 0,
                },
                extras = new glTFPrimitives_extras
                {
                    targetNames = new List<String>
                    {
                        "aaa",
                    }
                }
            };

            var f = new JsonFormatter();
            f.Serialize(prim);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""mode"":0,""attributes"":{""POSITION"":0},""material"":0,""extras"":{""targetNames"":[""aaa""]},""extensions"":{}}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFPrimitives>().Serialize(prim, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void AttributesTest()
        {
            var attrs = new glTFAttributes
            {
                POSITION = 0,
            };

            var f = new JsonFormatter();
            f.Serialize(attrs);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""POSITION"":0}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFAttributes>().Serialize(attrs, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void TextureInfoTest()
        {
            var texi = new glTFMaterialBaseColorTextureInfo()
            {
                index = 1,
            };

            var f = new JsonFormatter();
            f.Serialize(texi);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""index"":1,""texCoord"":0}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFMaterialBaseColorTextureInfo>().Serialize(texi, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void TextureInfoTestError()
        {
            var texi = new glTFMaterialBaseColorTextureInfo();

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var ex = Assert.Throws<JsonSchemaValidationException>(
                () => JsonSchema.FromType<glTFMaterialBaseColorTextureInfo>().Serialize(texi, c)
            );
            Assert.AreEqual("[index.String] minimum: ! -1>=0", ex.Message);
        }

        [Test]
        public void MaterialTest()
        {
            var texi = new glTFMaterial()
            {
                name = "a",
                emissiveFactor = new float[] { 0.5f, 0.5f, 0.5f },
            };

            var f = new JsonFormatter();
            f.Serialize(texi);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""name"":""a"",""emissiveFactor"":[0.5,0.5,0.5],""alphaCutoff"":0.5,""doubleSided"":false}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFMaterial>().Serialize(texi, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void GlTFToJsonTest()
        {
            var gltf = new glTF();
            using (var exporter = new gltfExporter(gltf))
            {
                exporter.Prepare(CreateSimpleScene());
                exporter.Export();
            }

            var expected = gltf.ToJson().ParseAsJson();
            expected.AddKey(Utf8String.From("meshes"));
            expected.AddValue(default(ArraySegment<byte>), ValueNodeType.Array);
            expected["meshes"].AddValue(default(ArraySegment<byte>), ValueNodeType.Object);

            var mesh = expected["meshes"][0];
            mesh.AddKey(Utf8String.From("name"));
            mesh.AddValue(Utf8String.From(JsonString.Quote("test")).Bytes, ValueNodeType.String);
            mesh.AddKey(Utf8String.From("primitives"));
            mesh.AddValue(default(ArraySegment<byte>), ValueNodeType.Array);
            mesh["primitives"].AddValue(default(ArraySegment<byte>), ValueNodeType.Object);

            var primitive = mesh["primitives"][0];
            primitive.AddKey(Utf8String.From("mode"));
            primitive.AddValue(Utf8String.From("0").Bytes, ValueNodeType.Integer);
            primitive.AddKey(Utf8String.From("indices"));
            primitive.AddValue(Utf8String.From("0").Bytes, ValueNodeType.Integer);
            primitive.AddKey(Utf8String.From("material"));
            primitive.AddValue(Utf8String.From("0").Bytes, ValueNodeType.Integer);
            primitive.AddKey(Utf8String.From("attributes"));
            primitive.AddValue(default(ArraySegment<byte>), ValueNodeType.Object);
            primitive["attributes"].AddKey(Utf8String.From("POSITION"));
            primitive["attributes"].AddValue(Utf8String.From("0").Bytes, ValueNodeType.Integer);
            primitive.AddKey(Utf8String.From("targets"));
            primitive.AddValue(default(ArraySegment<byte>), ValueNodeType.Array);
            primitive["targets"].AddValue(default(ArraySegment<byte>), ValueNodeType.Object);
            primitive["targets"][0].AddKey(Utf8String.From("POSITION"));
            primitive["targets"][0].AddValue(Utf8String.From("1").Bytes, ValueNodeType.Integer);
            primitive["targets"].AddValue(default(ArraySegment<byte>), ValueNodeType.Object);
            primitive["targets"][1].AddKey(Utf8String.From("POSITION"));
            primitive["targets"][1].AddValue(Utf8String.From("2").Bytes, ValueNodeType.Integer);
            primitive["targets"][1].AddKey(Utf8String.From("TANGENT"));
            primitive["targets"][1].AddValue(Utf8String.From("0").Bytes, ValueNodeType.Integer);

            gltf.meshes.Add(new glTFMesh("test")
            {
                primitives = new List<glTFPrimitives>
                {
                    new glTFPrimitives
                    {
                        indices = 0,
                        attributes = new glTFAttributes
                        {
                            POSITION = 0,
                            TANGENT = -1 // should be removed
                        },
                        targets = new List<gltfMorphTarget>
                        {
                            new gltfMorphTarget
                            {
                                POSITION = 1,
                                TANGENT = -1 // should be removed
                            },
                            new gltfMorphTarget
                            {
                                POSITION = 2,
                                TANGENT = 0
                            }
                        }
                    }
                }
            });
            var actual = gltf.ToJson().ParseAsJson();

            Assert.AreEqual(expected, actual);
        }

        public void MaterialTestError()
        {
            var texi = new glTFMaterial()
            {
                name = "b",
                emissiveFactor = new float[] { 1.5f, 0.5f, 0.5f },
            };

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var ex = Assert.Throws<JsonSchemaValidationException>(
                () => JsonSchema.FromType<glTFMaterial>().Serialize(texi, c)
            );
            Assert.AreEqual("[emissiveFactor.String] maximum: ! 1.5<=1", ex.Message);
        }

        [Test]
        public void NodeTest()
        {
            var texi = new glTFNode()
            {
                name = "a",
                skin = 0,
                camera = -1,
            };

            var f = new JsonFormatter();
            f.Serialize(texi);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""name"":""a"",""skin"":0,""extras"":{}}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFNode>().Serialize(texi, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void NodeTestError()
        {
            var texi = new glTFNode()
            {
                name = "a",
                camera = -2,
            };

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var ex = Assert.Throws<JsonSchemaValidationException>(
                () => JsonSchema.FromType<glTFNode>().Serialize(texi, c)
            );
            Assert.AreEqual("[camera.String] minimum: ! -2>=0", ex.Message);
        }

        [Test]
        public void SkinTest()
        {
            var texi = new glTFSkin()
            {
                name = "b",
                joints = new int[] {1},
            };

            var f = new JsonFormatter();
            f.Serialize(texi);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            Assert.AreEqual(@"{""joints"":[1],""name"":""b""}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFSkin>().Serialize(texi, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void SkinTestEmptyName()
        {
            var texi = new glTFSkin()
            {
                name = "",
                joints = new int[] {1},
            };

            var f = new JsonFormatter();
            f.Serialize(texi);

            var json = new Utf8String(f.GetStoreBytes()).ToString();
            // "name" = "", not excluded
            Assert.AreEqual(@"{""joints"":[1],""name"":""""}", json);
            Debug.Log(json);

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var json2 = JsonSchema.FromType<glTFSkin>().Serialize(texi, c);
            Assert.AreEqual(json, json2);
        }

        [Test]
        public void SkinTestErrorNull()
        {
            var texi = new glTFSkin()
            {
                name = "b",
                joints = null,
            };

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var ex = Assert.Throws<JsonSchemaValidationException>(
                () => JsonSchema.FromType<glTFSkin>().Serialize(texi, c)
            );
            Assert.AreEqual("[joints.String] null", ex.Message);
        }

        [Test]
        public void SkinTestError()
        {
            var texi = new glTFSkin()
            {
                name = "b",
                joints = new int[] {},
            };

            var c = new JsonSchemaValidationContext("")
            {
                EnableDiagnosisForNotRequiredFields = true,
            };
            var ex = Assert.Throws<JsonSchemaValidationException>(
                () => JsonSchema.FromType<glTFSkin>().Serialize(texi, c)
            );
            Assert.AreEqual("[joints.String] minItems", ex.Message);
        }
    }
}
