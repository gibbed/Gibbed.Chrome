/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

namespace Gibbed.Chrome.FileFormats.ResourcePackage
{
    public enum ResourceType : byte
    {
        Invalid = 0,
        
        Mesh = 16,
        MeshFixups = 17,
        
        Skin = 18,
        SkinFixups = 19,
        
        Texture = 32,
        TextureBitmapData = 33,
        TextureMipBitmapData = 34,
        
        Material = 48,
        Shader = 49,
        MaterialFixups = 50,
        MaterialTextures = 51,
        
        Animation = 64,
        AnimationStream = 65,
        AnimationScript = 66,
        AnimationScriptFixups = 67,

        Fx = 80,
        Lightmap = 96,
        Flash = 97,
        
        Sound = 101,
        Music = 102,
        Speech = 103,
        SFXStream = 104,
        SFXLocal = 105,

        DensityMap = 112,
        HeightMap = 128,
        Mimics = 144,
        PathMap = 160,
        Phonemes = 176,
        
        StaticGeometry = 192,
        StaticGeometrySetup = 193,
        StaticGeometryFixups = 194,
        StaticGeometrySetupFixups = 195,

        TextData = 208,
        BinaryData = 224,
        VertexData = 240,
        IndexData = 241,
        VertexDynamicData = 242,

        TinyObjects = 248,
        TinyObjectsFixUps = 249,
        TinyObjectsDensityMap = 250,

        BuilderInformation = 255,
    }
}
