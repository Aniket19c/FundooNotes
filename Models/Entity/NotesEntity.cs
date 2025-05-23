﻿using Models.Entity;
using Repository.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

public class NotesEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NoteId { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Reminder { get; set; }
    public string? Backgroundcolor { get; set; }
    public string? Image { get; set; }

    [DefaultValue(false)]
    public bool Pin { get; set; }

    public DateTime Created { get; set; }
    public DateTime Edited { get; set; }

    [DefaultValue(false)]
    public bool Trash { get; set; }

    [DefaultValue(false)]
    public bool Archieve { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public UserEntity? User { get; set; }


    public ICollection<NoteLabelEntity> NoteLabels { get; set; }
}
