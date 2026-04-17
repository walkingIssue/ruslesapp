---
title: "Documenting the geospatial pipeline and tightening MVP privacy assumptions"
date: "2026-04-17"
slug: "documenting-the-geospatial-pipeline-and-tightening-mvp-privacy-assumptions"
summary: "Replace with a short summary."
draft: false
---

## Clarified the geospatial docs

The scratchpad repo was used to create a concrete benchmarking project.
I noticed that propper batching within the spec read can increase the efficiency significantly per read.

So the geospatial resolution pipeline was optimized and completely rewritten to support a better batchng model.

this was a extremely hard task where almost a full week went into just plainly staring at the code looking for solutions.
I'm glad the bulk of the work is done, alas, the testing spec has to improve so we can be sure it follows the new expected contracts and behavior

## Resolution pipeline responsibilities are being separated

The rewrite introduced a host of issues that were never considered in the original. 
I wouldn't say the issues weren't there, but the simplified implementation allowed me to bypass alot of them.
Alas the effeciency suffered.

With this current setup, once testing has been fully fleshed out. Our effeciency on resolution coorindianate resolution is much greater.
The pipeline now has a split across userrequest lifetimes, users connected and their rough location.

And we can be sure that you always get most locally viable information.

## MVP specs and privacy direction

I'm not a lawyer and i'm not a gdpr officer that's fully capable of creating a privacy policy.
But we are taking steps to 1. Comply with GDPR to the best of our ability and prepare us for a full launch and hiring an officer. Docks that are meant to be reviewed by every commit to main
have been prepared so the developer and privacy officer can in conjuction review how the new changes will affect our privacy policy.

Our mvp spec now states that we need to have a plain languge gpr policy, we will also have plain language Acceptable Use, and community guidlines policy that users will need to accept before creating an account.
This affects the gdpr policy as we need an attest timestamp by the user.

We will also allow users to block other users and regret their choice as part of the mvp.

The mvp will also firt be released to the internet and browser due to it being a simpler release target. Once we worked out the kinks of the platform on the web we will start to focus on a mobile release. 

## Release status

The first webversion _Alpha release_ will be within the next couple of weeks. The platform will not be directly accessible to the public but soon after if no big issues arise that entail a huge rewrite of the codebase.
within a couple of weeks of the Alpha we will commence a publicly open _beta_!  

